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
        
        Eat,
        
        /// <summary>
        /// Eats one token and patches it into the given kind.
        /// </summary>
        EatAs,
        
        /// <summary>
        /// Inserts a missing token with specified kind.
        /// </summary>
        Make
    }

    private readonly record struct ParseEvent(
        ParseEventKind EventKind,
        SyntaxKind? SyntaxKind = null,
        TokenKind? TokenKind = null);

    private readonly record struct MarkOpen(int OpenIndex);

    private readonly record struct MarkClose(int OpenIndex);

    
    private sealed class Scanner
    {
        /// <summary>
        /// Custom enumerator that asserts the parser has eaten at least one
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
                    throw new ParserStuckException("Parser did not eat a token and is stuck.");

                _lastToken = scanner._nextToken;
                return true;
            }

            public readonly LoopGuard GetEnumerator() => this;
        }

        
        /// <summary>
        /// Amount of <see cref="Peek"/>s allowed between two <see cref="Eat"/>s.
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
        /// Backstop for everything <see cref="MustEatEachIteration"/> cannot see:
        /// hand-written loops and recursion that re-enters without consuming a token.
        /// Refilled by <see cref="Eat"/>, burned by <see cref="Peek"/>.
        /// </summary>
        private int _fuel;


        /// <summary>
        /// All tokens, including trivia.
        /// </summary>
        public ImmutableArray<Token> AllTokens { get; }

        public bool IsAtEnd => IsAt(TokenKind.Eof);

        public int Position => _nextToken;


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
        /// Returns a custom enumerator that asserts the parser has eaten at least one
        /// token inside the loop. Otherwise, throws <see cref="ParserStuckException"/>.
        /// Stops at Eof.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var _ in scanner.MustEatEachIteration())
        /// {
        ///     ...
        /// }
        /// </code>
        /// </example>
        public LoopGuard MustEatEachIteration()
            => new(this);
        
        public Token? Last
            => _nextToken > 0 ? _tokens[_nextToken - 1] : null;

        
        public MarkOpen Open()
        {
            _events.Add(new ParseEvent(ParseEventKind.Open));
            return new MarkOpen(_events.Count - 1);
        }

        public MarkClose Close(MarkOpen openMark, SyntaxKind kind)
        {
            // No event between Open and Close means the node has no children at all.
            // Every node must cover at least one token (missing or not).
            Debug.Assert(_events.Count > openMark.OpenIndex + 1, 
                "Closed an empty node.");

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


        /// <summary>
        /// Creates a missing token of <paramref name="kind"/>.
        /// </summary>
        public void Make(TokenKind kind)
        {
            _events.Add(new ParseEvent(ParseEventKind.Make, TokenKind: kind));
        }

        /// <summary>
        /// Creates a node of <paramref name="nodeKind"/> with a missing token of <paramref name="tokenKind"/>.
        /// </summary>
        public MarkClose MakeIntoNode(TokenKind tokenKind, SyntaxKind nodeKind)
        {
            var node = Open();
            Make(tokenKind);
            return Close(node, nodeKind);
        }
        
        
        public Token Eat()
        {
            Debug.Assert(_nextToken < _tokens.Count);

            _events.Add(new ParseEvent(ParseEventKind.Eat));
            _fuel = MaxFuel;
            return _tokens[_nextToken++];
        }

        /// <summary>
        /// Same as <see cref="Eat"/>, but assert, that <paramref name="knownKind"/>
        /// was eaten.
        /// </summary>
        public Token EatKnown(TokenKind knownKind)
        {
            var token = Eat();
            Debug.Assert(token.Kind == knownKind);
            return token;
        }

        /// <summary>
        /// Eats the next token and rewrites its <see cref="TokenKind"/>
        /// to <paramref name="kind"/>. <paramref name="kind"/> must be a
        /// token that doesn't carry a value.
        /// </summary>
        public Token EatAs(TokenKind kind)
        {
            Debug.Assert(!kind.HasValue);
            
            _events.Add(new ParseEvent(ParseEventKind.EatAs, TokenKind: kind));
            _fuel = MaxFuel;
            return _tokens[_nextToken++];
        }

        public MarkClose EatIntoNode(SyntaxKind nodeKind)
        {
            var node = Open();
            Eat();
            return Close(node, nodeKind);
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

        public bool IsAt(Anchor anchor)
            => anchor.Contains(Peek().Kind);

        public bool IsAt(TokenSet set)
            => set.Contains(Peek().Kind);
    }
}