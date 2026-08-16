using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    public sealed class ParserStuckException(string message)
        : Exception(message);

    private enum ExpectedSyntaxErrorContext
    {
        MissingBefore,
        MissingAfter,
        Unexpected,
        ManualReporting,
    }
    
    private abstract record ParseEvent
    {
        public sealed record Open : ParseEvent
        {
            /// <summary>
            /// <c>null</c> only, when node has not yet been closed.
            /// </summary>
            public SyntaxKind? Kind { get; set; } = null;

            /// <summary>
            /// If <see cref="Kind"/> is <see cref="SyntaxKind.Error"/>, this is
            /// the <see cref="Diagnostic"/>, that explains the error.
            /// <c>null</c> for other kinds and for still unexplained errors.
            /// During tree-building, every error must be explained.
            /// </summary>
            public Diagnostic.Error? ExplainingError { get; set; } = null;
        }

        public sealed record Close(Open OpenEvent) : ParseEvent;

        public sealed record Eat : ParseEvent;

        /// <summary>
        /// Eats one token and patches it into <paramref name="Kind"/>.
        /// </summary>
        public sealed record EatAs(TokenKind Kind) : ParseEvent;

        /// <summary>
        /// Makes a missing token of <paramref name="Kind"/>.
        /// </summary>
        public sealed record Make(TokenKind Kind, Diagnostic.Error ExplainingError) : ParseEvent;
    }
    

    private readonly record struct MarkOpen(int OpenEventIndex);

    private readonly record struct MarkClose(int OpenEventIndex);

    
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
        private readonly SourceFileView _source;
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
            _events.Add(new ParseEvent.Open());
            return new MarkOpen(_events.Count - 1);
        }

        public MarkClose Close(MarkOpen openMark, SyntaxKind kind)
        {
            Guard.MustBe(kind is not SyntaxKind.Error, $"Construct through {nameof(CloseAsError)}");
            Debug.Assert(_events[(openMark.OpenEventIndex + 1)..]
                .Any(ev => ev is ParseEvent.Eat or ParseEvent.EatAs or ParseEvent.Make),
                "Closed a node which has no tokens.");

            var openEvent = _events[openMark.OpenEventIndex] as ParseEvent.Open;
            Debug.Assert(openEvent is not null, $"{nameof(openMark.OpenEventIndex)} was not an open event.");
            
            openEvent.Kind = kind;
            _events.Add(new ParseEvent.Close(openEvent));
            return new MarkClose(openMark.OpenEventIndex);
        }


        /// <summary>
        /// Explains the error as <see cref="Diagnostic.UnexpectedToken"/> on the first token
        /// inside.
        /// </summary>
        public MarkClose CloseAsError(MarkOpen openMark, ExpectedSyntax expectedSyntax)
        {
            // Find first token that was eaten inside the error
            var openNode = _events[openMark.OpenEventIndex] as ParseEvent.Open;
            Debug.Assert(openNode is not null);

            var eatCount = _events[(openMark.OpenEventIndex + 1)..]
                .Count(e => e is ParseEvent.Eat or ParseEvent.EatAs);
            Debug.Assert(eatCount > 0, "Closed a node which has no tokens.");

            var firstTokenIndex = _nextToken - eatCount;
            var unexpectedToken = _tokens[firstTokenIndex];
            var unexpectedError = new Diagnostic.UnexpectedToken(_source, unexpectedToken, expectedSyntax);
            return CloseAsError(openMark, unexpectedError);
        }
        
        public MarkClose CloseAsError(MarkOpen openMark, Diagnostic.Error explanation)
        {
            var errorNode = CloseAsUnexplainedError(openMark);
            ExplainError(errorNode, explanation);
            return errorNode;
        }
        
        /// <summary>
        /// Closes <paramref name="openMark"/> as <see cref="SyntaxKind.Error"/> and leaves
        /// it unexplained. Needs to be explained through <see cref="ExplainError"/> before
        /// tree-building.
        /// </summary>
        public MarkClose CloseAsUnexplainedError(MarkOpen openMark)
        {
            Debug.Assert(_events[(openMark.OpenEventIndex + 1)..]
                    .Any(ev => ev is ParseEvent.Eat or ParseEvent.EatAs),
                "Closed an error node which has not eaten tokens.");

            var openEvent = _events[openMark.OpenEventIndex] as ParseEvent.Open;
            Debug.Assert(openEvent is not null, $"{nameof(openMark.OpenEventIndex)} was not an open event.");
            
            openEvent.Kind = SyntaxKind.Error;
            openEvent.ExplainingError = null;
            
            _events.Add(new ParseEvent.Close(openEvent));
            return new MarkClose(openMark.OpenEventIndex);
        }

        public void ExplainError(MarkClose errorNode, Diagnostic.Error explanation)
        {
            var openEvent = _events[errorNode.OpenEventIndex] as ParseEvent.Open;
            Debug.Assert(openEvent?.Kind is SyntaxKind.Error, $"{nameof(errorNode)} not an open event with kind error.");
            Debug.Assert(openEvent.ExplainingError is null, "Error already explained.");

            openEvent.ExplainingError = explanation;
        }

        
        /// <summary>
        /// Requires a <see cref="MarkClose"/>, because the mark will be invalidated!
        /// </summary>
        public MarkOpen OpenBefore(MarkClose before)
        {
            Debug.Assert(_events[before.OpenEventIndex] is ParseEvent.Open);
            _events.Insert(before.OpenEventIndex, new ParseEvent.Open());
            return new MarkOpen(before.OpenEventIndex);
        }



        /// <summary>
        /// Creates a missing token of <paramref name="kind"/> and explains
        /// it with <paramref name="explanation"/>.
        /// </summary>
        public void Make(TokenKind kind, Diagnostic.Error explanation)
            => _events.Add(new ParseEvent.Make(kind, explanation));

        /// <summary>
        /// Creates a missing token of <paramref name="kind"/> and explains it
        /// as <see cref="Diagnostic.MissingToken"/> on the next token.
        /// </summary>
        /// <param name="expectedSyntax"><c>null</c>: <paramref name="kind"/> will be used.</param>
        public void Make(TokenKind kind, ExpectedSyntax? expectedSyntax = null)
            => Make(kind, new Diagnostic.MissingToken(
                _source,
                Previous: _nextToken > 0 ? _tokens[_nextToken - 1] : null,
                Next: Peek(),
                Expected: expectedSyntax ?? kind));
        

        /// <summary>
        /// Creates a node of <paramref name="nodeKind"/> with a missing token of <paramref name="tokenKind"/>.
        /// </summary>
        public MarkClose MakeIntoNode(TokenKind tokenKind, SyntaxKind nodeKind, ExpectedSyntax? expectedSyntax)
        {
            var node = Open();
            Make(tokenKind, expectedSyntax);
            return Close(node, nodeKind);
        }
        
        
        public Token Eat()
        {
            Debug.Assert(_nextToken < _tokens.Count);

            _events.Add(new ParseEvent.Eat());
            _fuel = MaxFuel;
            return _tokens[_nextToken++];
        }
        
        public MarkClose EatIntoNode(SyntaxKind nodeKind)
        {
            Guard.MustBe(nodeKind is not SyntaxKind.Error, $"Must be constructed through {nameof(EatIntoErrorNode)}");
            var node = Open();
            Eat();
            return Close(node, nodeKind);
        }
        
        public MarkClose EatIntoErrorNode(ExpectedSyntax expectedSyntax)
        {
            var error = Open();
            Eat();
            return CloseAsError(error, expectedSyntax);
        }

        /// <summary>
        /// Same as <see cref="Eat"/>, but asserts, that <paramref name="knownKind"/>
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
        public void EatAs(TokenKind kind)
        {
            Debug.Assert(!kind.HasValue);
            
            _events.Add(new ParseEvent.EatAs(kind));
            _fuel = MaxFuel;
            _nextToken++;
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