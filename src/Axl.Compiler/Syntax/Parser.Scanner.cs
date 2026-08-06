using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    public sealed class ParserStuckException(string message)
        : Exception(message);
    
    private enum ParseEventKind
    {
        Open,
        Close,
        Advance,
    }

    /// <param name="SyntaxKind">
    /// Only meaningful on <see cref="ParseEventKind.Open"/>.
    /// <c>null</c> if no kind has been assigned yet.
    /// </param>
    private readonly record struct ParseEvent(ParseEventKind EventKind, SyntaxKind? SyntaxKind = null)
    {
        public override string ToString()
            => EventKind switch
            {
                ParseEventKind.Open => $"Open {SyntaxKind?.ToString() ?? "?"}",
                ParseEventKind.Close => "Close",
                ParseEventKind.Advance => "Advance",
                _ => throw new UnreachableException()
            };
    }

    private readonly record struct MarkOpen(int OpenIndex);

    private readonly record struct MarkClose(int OpenIndex);

    
    private sealed class Scanner
    {
        /// <summary>
        /// Custom enumerator that asserts the parser has advanced at least one
        /// token inside the loop. Otherwise, throws <see cref="ParserStuckException"/>.
        /// It stops at Eof.
        /// </summary>
        public ref struct LoopGuard(Scanner scanner)
        {
            private int _lastToken = -1;

            public readonly int Current => _lastToken;
            
            [DebuggerHidden]
            [StackTraceHidden]
            public bool MoveNext()
            {
                if (scanner.IsAtEnd)
                    return false;
                
                Debug.Assert(scanner._nextToken >= _lastToken, "Scanner moved backwards. Weird!");
                if (scanner._nextToken == _lastToken)
                    throw new ParserStuckException("Parser did not advance a token and is stuck.");

                _lastToken = scanner._nextToken;
                return true;
            }

            public readonly LoopGuard GetEnumerator() => this;
        }

        
        /// <summary>
        /// Amount of <see cref="Peek"/>s allowed between two <see cref="Advance"/>s.
        /// Generous - real lookahead never goes beyond a handful.
        /// </summary>
        private const int MaxFuel = 256;

        /// <summary>
        /// Only non-trivia tokens. Must not be modified, but is kept as a list
        /// to avoid another allocation.
        /// </summary>
        private readonly List<Token> _tokens;
        private readonly List<ParseEvent> _events;
        private int _nextToken;

        /// <summary>
        /// Backstop for everything <see cref="MustAdvanceUntilEnd"/> cannot see:
        /// hand-written loops and recursion that re-enters without consuming a token.
        /// Refilled by <see cref="Advance"/>, burned by <see cref="Peek"/>.
        /// </summary>
        private int _fuel;


        /// <summary>
        /// All tokens, including trivia.
        /// </summary>
        public ImmutableArray<Token> AllTokens { get; }

        public bool IsAtEnd => IsAt(TokenKind.Eof);


        public Scanner(ImmutableArray<Token> tokens)
        {
            AllTokens = tokens;

            // This list will be oversized by exactly the amount of trivia tokens.
            // If that every shows up, we could count non-trivia tokens before.
            // But I don't think this will ever show up.
            _tokens = new List<Token>(capacity: tokens.Length);
            foreach (var token in tokens)
            {
                if (!token.Kind.IsTrivia)
                    _tokens.Add(token);
            }

            _events = [];
            _nextToken = 0;
            _fuel = MaxFuel;
        }


        public IEnumerable<ParseEvent> GetEvents()
            => _events;

        
        /// <summary>
        /// Returns a custom enumerator that asserts the parser has advanced at least one
        /// token inside the loop. Otherwise, throws <see cref="ParserStuckException"/>.
        /// Stops at Eof.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var _ in scanner.MustAdvanceUntilEnd())
        /// {
        ///     ...
        /// }
        /// </code>
        /// </example>
        public LoopGuard MustAdvanceUntilEnd()
            => new(this);
        
        public Token? PreviousToken
            => _nextToken > 0 ? _tokens[_nextToken - 1] : null;

        public MarkOpen Open()
        {
            _events.Add(new ParseEvent(ParseEventKind.Open));
            return new MarkOpen(_events.Count - 1);
        }

        public MarkClose Close(MarkOpen openMark, SyntaxKind kind)
        {
            _events[openMark.OpenIndex] = new ParseEvent(ParseEventKind.Open, kind);
            _events.Add(new ParseEvent(ParseEventKind.Close));
            return new MarkClose(openMark.OpenIndex);
        }

        /// <summary>
        /// Requires a <see cref="MarkClose"/>, because the mark will be invalidated!
        /// </summary>
        public MarkOpen OpenBefore(MarkClose before)
        {
            _events.Insert(before.OpenIndex, new ParseEvent(ParseEventKind.Open));
            return new MarkOpen(before.OpenIndex);
        }

        public Token Advance()
        {
            Debug.Assert(!IsAtEnd);

            _events.Add(new ParseEvent(ParseEventKind.Advance));
            _fuel = MaxFuel;
            return _tokens[_nextToken++];
        }

        
        public Token Peek(int lookahead = 0)
        {
            if (--_fuel < 0)
                throw new ParserStuckException("Parser peeked too often without advancing and is stuck.");
            return UnsafePeek(lookahead);
        }

        /// <summary>
        /// Peeks while circumventing the infinite loop protection. Use
        /// with caution and only when the loop is bounded naturally.
        /// </summary>
        public Token UnsafePeek(int lookahead = 0)
        {
            Debug.Assert(lookahead >= 0);

            if (_nextToken + lookahead < _tokens.Count)
                return _tokens[_nextToken + lookahead];

            Debug.Assert(_tokens[^1].Kind is TokenKind.Eof);
            return _tokens[^1];
        }

        
        public bool IsAt(TokenKind kind)
            => Peek().Kind == kind;

        public bool IsAt(TokenSet set)
            => set.Contains(Peek().Kind);

        public bool TryAdvance(TokenKind expectedKind)
        {
            if (IsAt(expectedKind))
            {
                Advance();
                return true;
            }

            return false;
        }

        public Token AdvanceKnown(TokenKind knownKind)
        {
            Debug.Assert(IsAt(knownKind));
            return Advance();
        }


        /// <summary>
        /// Meant to be executed from the debugger for a better view
        /// of the parsers events.
        /// </summary>
        /// <returns></returns>
        internal string ToDebugString(SourceFileView source)
        {
            var tokenIndex = 0;
            var builder = new StringBuilder();
            foreach (var e in _events)
            {
                if (e.EventKind is ParseEventKind.Advance)
                {
                    var text = source.GetText(_tokens[tokenIndex].Span);
                    builder.Append($"\'{text}\' ");
                    tokenIndex++;
                }
                else if (e.EventKind is ParseEventKind.Open)
                {
                    builder.Append($"[{e.SyntaxKind?.ToString() ?? "?"} ");
                }
                else
                {
                    builder.Append("]");
                }
            }

            return builder.ToString();
        }
    }
}