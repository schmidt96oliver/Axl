using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    public sealed class ParserStuckException(string message)
        : Exception(message);
    
    /// <summary>
    /// Custom enumerator that asserts the parser has eaten at least one
    /// token inside the loop. Otherwise, throws <see cref="ParserStuckException"/>.
    /// It stops at Eof.
    /// </summary>
    private ref struct LoopGuard(Scanner scanner)
    {
        private int _lastToken = -1;

        public readonly int Current => _lastToken;
            
        [DebuggerHidden]
        [StackTraceHidden]
        public bool MoveNext()
        {
            if (scanner.IsAtEnd)
                return false;
                
            Debug.Assert(scanner.Position >= _lastToken, "Scanner moved backwards. Weird!");
            if (scanner.Position == _lastToken)
                throw new ParserStuckException("Parser did not eat a token and is stuck.");

            _lastToken = scanner.Position;
            return true;
        }

        public readonly LoopGuard GetEnumerator() => this;
    }

    
    /// <summary>
    /// Range of scanner positions claimed by one <see cref="ParseError.Report"/>.
    /// </summary>
    private readonly record struct ClaimedRange(int First, int Last);
    
    private abstract record ParseEvent
    {
        public sealed record Open : ParseEvent
        {
            /// <summary>
            /// <c>null</c> only, when node has not yet been closed.
            /// </summary>
            public SyntaxKind? Kind { get; set; } = null;
        }

        public sealed record Close : ParseEvent;

        public sealed record Eat : ParseEvent;

        /// <summary>
        /// Eats one token and patches it into <paramref name="Kind"/>.
        /// </summary>
        public sealed record EatAs(TokenKind Kind) : ParseEvent;

        /// <summary>
        /// Makes a missing token of <paramref name="Kind"/>.
        /// </summary>
        public sealed record Make(TokenKind Kind) : ParseEvent;

        /// <summary>
        /// Reports an <paramref name="Error"/>. Might be suppressed, if another <see cref="Report"/> already
        /// claimed that range through its own <paramref name="ClaimedRange"/>.
        /// </summary>
        /// <param name="ClaimedRange">
        /// Range, this diagnostics explains.
        /// Succeeding diagnostics within this range that are suppressible
        /// will be suppressed.
        /// </param>
        /// <param name="IsSuppressible"><c>False</c> will always report this diagnostic.</param>
        public sealed record Report(Diagnostic.Error Error, ClaimedRange ClaimedRange, bool IsSuppressible) : ParseEvent;
    }
    

    private readonly record struct MarkOpen(int OpenEventIndex);

    private readonly record struct MarkClose(int OpenEventIndex);

    
    private sealed class Scanner
    {
        /// <summary>
        /// Amount of <see cref="Peek"/>s allowed between two <see cref="Eat"/>s.
        /// Generous - real lookahead never goes beyond a handful.
        /// </summary>
        private const int MaxFuel = 256;

        /// <summary>
        /// Backstop for everything <see cref="MustEatEachIteration"/> cannot see:
        /// handwritten loops and recursion that re-enters without consuming a token.
        /// Refilled by <see cref="Eat"/>, burned by <see cref="Peek"/>.
        /// </summary>
        private int _fuel;
        
        /// <summary>
        /// Only non-trivia tokens. Must not be modified, but is kept as a list
        /// to avoid another allocation.
        /// </summary>
        private readonly List<Token> _tokens;
        private readonly List<ParseEvent> _events;
        private readonly SourceFileView _source;

        
        /// <summary>
        /// All tokens, including trivia.
        /// </summary>
        public ImmutableArray<Token> AllTokens { get; }

        /// <summary>
        /// Position is always a gap between tokens. Position <c>i</c> means, the
        /// scanner is sitting before non-trivia token <c>i</c>.
        /// </summary>
        public int Position { get; private set; }

        public Token? Last => Position > 0 ? _tokens[Position - 1] : null;

        public bool IsAtEnd => IsAt(TokenKind.Eof);
        

        public Scanner(SourceFileView source, ImmutableArray<Token> tokens)
        {
            _source = source;
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
            Position = 0;
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
        
        
        public MarkOpen Open()
        {
            _events.Add(new ParseEvent.Open());
            return new MarkOpen(_events.Count - 1);
        }

        public MarkClose Close(MarkOpen openMark, SyntaxKind kind)
        {
            Debug.Assert(_events[(openMark.OpenEventIndex + 1)..]
                .Any(ev => ev is ParseEvent.Eat or ParseEvent.EatAs or ParseEvent.Make),
                "Closed a node which has no tokens.");

            var openEvent = _events[openMark.OpenEventIndex] as ParseEvent.Open;
            Debug.Assert(openEvent is not null, $"{nameof(openMark.OpenEventIndex)} was not an open event.");
            
            openEvent.Kind = kind;
            _events.Add(new ParseEvent.Close());
            return new MarkClose(openMark.OpenEventIndex);
        }

        public MarkOpen OpenBefore(MarkClose before)
        {
            Debug.Assert(_events[before.OpenEventIndex] is ParseEvent.Open);
            _events.Insert(before.OpenEventIndex, new ParseEvent.Open());
            return new MarkOpen(before.OpenEventIndex);
        }

        public Token Eat()
        {
            Debug.Assert(Position < _tokens.Count);

            _events.Add(new ParseEvent.Eat());
            _fuel = MaxFuel;
            return _tokens[Position++];
        }
        
        /// <summary>
        /// Same as <see cref="Eat"/>, but asserts, that <paramref name="knownKind"/>
        /// was eaten.
        /// </summary>
        public void EatKnown(TokenKind knownKind)
        {
            var token = Eat();
            Debug.Assert(token.Kind == knownKind);
        }
        
        /// <summary>
        /// Eats the next token and rewrites its <see cref="TokenKind"/>
        /// to <paramref name="kind"/>. Only for tokens that don't carry a value.
        /// </summary>
        public void EatAs(TokenKind kind)
        {
            Debug.Assert(!kind.HasValue);
            
            _events.Add(new ParseEvent.EatAs(kind));
            _fuel = MaxFuel;
            Position++;
        }
        
        
        /// <summary>
        /// Creates a missing token of <paramref name="kind"/> and reports a
        /// <see cref="Diagnostic.MissingToken"/> on the next token.
        /// </summary>
        /// <param name="expectedSyntax"><c>null</c>: <paramref name="kind"/> will be used.</param>
        public void MakeAndReport(TokenKind kind, ExpectedSyntax? expectedSyntax = null)
        {
            ReportMissingTokenHere(expectedSyntax ?? kind);
            _events.Add(new ParseEvent.Make(kind));
        }
        
        
        public MarkClose EatInto(SyntaxKind nodeKind)
        {
            var node = Open();
            Eat();
            return Close(node, nodeKind);
        }

        /// <summary>
        /// Eats the next token into a <see cref="SyntaxKind.Error"/> node and reports
        /// a <see cref="Diagnostic.UnexpectedToken"/>.
        /// </summary>
        public MarkClose EatIntoErrorAndReport(ExpectedSyntax expectedSyntax)
        {
            var node = EatInto(SyntaxKind.Error);
            ReportUnexpectedTokensUntilHere(Position, expectedSyntax);
            return node;
        }

        
        private void Report(Diagnostic.Error error, ClaimedRange range, bool isSuppressible = false)
            => _events.Add(new ParseEvent.Report(error, range, isSuppressible));

        public void ReportHere(Diagnostic.Error error, bool isSuppressible = false)
            => Report(error, new ClaimedRange(Position, Position), isSuppressible);
        
        public void ReportMissingTokenHere(ExpectedSyntax expectedSyntax)
        {
            var error = new Diagnostic.MissingToken(
                _source,
                Previous: Last,
                Next: Peek(),
                expectedSyntax);
            ReportHere(error, isSuppressible: true);
        }

        public void ReportUnexpectedTokensUntilHere(int firstClaimedGap, ExpectedSyntax expectedSyntax)
        {
            Guard.InRange(firstClaimedGap > 0);
            Guard.InRange(firstClaimedGap <= Position);
            
            var token = _tokens[firstClaimedGap - 1];
            var error = new Diagnostic.UnexpectedToken(_source, token, expectedSyntax);
            Report(error,
                new ClaimedRange(firstClaimedGap, Position),
                isSuppressible: true);
        }

        
        /// <param name="skipCount"><c>0</c> returns the next token. <c>1</c> the one thereafter and so on.</param>
        public Token Peek(int skipCount = 0)
        {
            if (--_fuel < 0)
                throw new ParserStuckException("Parser peeked too often without advancing and is stuck.");
            return UnsafePeek(skipCount);
        }

        /// <summary>
        /// Peeks while circumventing the infinite loop protection. Use
        /// with caution and only when the loop is bounded naturally.
        /// </summary>
        public Token UnsafePeek(int lookahead = 0)
        {
            Debug.Assert(lookahead >= 0);

            if (Position + lookahead < _tokens.Count)
                return _tokens[Position + lookahead];

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