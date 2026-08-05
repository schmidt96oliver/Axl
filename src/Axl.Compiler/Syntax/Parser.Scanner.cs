using System.Collections.Immutable;
using System.Diagnostics;

namespace Axl.Compiler.Syntax;

public partial class Parser
{
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
    private readonly record struct ParseEvent(ParseEventKind EventKind, SyntaxKind SyntaxKind = SyntaxKind.Error);

    private readonly record struct MarkOpen(int OpenIndex);

    private readonly record struct MarkClose(int OpenIndex);

    private sealed class Scanner
    {
        /// <summary>
        /// Only non-trivia tokens. Must not be modified, but is kept as a list
        /// to avoid another allocation.
        /// </summary>
        private readonly List<Token> _tokens;
        private readonly List<ParseEvent> _events;
        private int _nextToken;


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
        }


        public IEnumerable<ParseEvent> GetEvents()
            => _events;


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
            return _tokens[_nextToken++];
        }

        public Token Peek(int lookahead = 1)
        {
            Debug.Assert(lookahead >= 0);
            if (_nextToken + lookahead < _tokens.Count)
                return _tokens[_nextToken + lookahead];

            Debug.Assert(_tokens[^1].Kind is TokenKind.Eof);
            return _tokens[^1];
        }


        public bool IsAt(TokenKind kind)
            => Peek(0).Kind == kind;

        public bool IsAt(TokenSet set)
            => set.Contains(Peek(0).Kind);

        public bool TryAdvance(TokenKind expectedKind)
        {
            if (IsAt(expectedKind))
            {
                Advance();
                return true;
            }

            return false;
        }

        public void AdvanceKnown(TokenKind knownKind)
        {
            Debug.Assert(IsAt(knownKind));
            Advance();
        }
    }
}