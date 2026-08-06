using System.Diagnostics;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Diagnostics;

public abstract partial record Diagnostic
{
    public sealed record UnclosedString(SourceFileView Source, Token LastToken) : Error
    {
        public override SourceLocation Location => Source.GetLocation(SourceSpan.EmptyAfter(LastToken.Span));
        public override string Message => "String has not been closed.";
    }

    public sealed record UnexpectedToken : Error
    {
        private readonly TokenKind? _expectedKind;
        private readonly SyntaxCategory? _expectedCategory;

        public SourceFileView Source { get; }
        public Token Actual { get; }


        public UnexpectedToken(SourceFileView source, Token actual, TokenKind expectedKind)
        {
            Source = source;
            Actual = actual;
            _expectedKind = expectedKind;
        }

        public UnexpectedToken(SourceFileView source, Token actual, SyntaxCategory expectedCategory)
        {
            Source = source;
            Actual = actual;
            _expectedCategory = expectedCategory;
        }


        public override SourceLocation Location => Source.GetLocation(Actual.Span);

        public override string Message
        {
            get
            {
                var expected = (_expectedKind, _expectedCategory) switch
                {
                    (TokenKind kind, null) => kind.DisplayName,
                    (null, SyntaxCategory.Expr) => "an expression",
                    (null, SyntaxCategory.Stmt) => "a statement",
                    _ => throw new UnreachableException(),
                };

                return $"Expected {expected}, got {Actual.Kind.DisplayName}.";
            }
        }
    }
    
    public sealed record MissingToken : Error
    {
        private readonly TokenKind? _expectedKind;
        private readonly SyntaxCategory? _expectedCategory;

        public SourceFileView Source { get; }
        public Token? Previous { get; }
        public Token Next { get; }


        public MissingToken(SourceFileView source, Token? previous, Token next, TokenKind expectedKind)
        {
            Source = source;
            Previous = previous;
            Next = next;
            _expectedKind = expectedKind;
        }

        public MissingToken(SourceFileView source, Token? previous, Token next, SyntaxCategory expectedCategory)
        {
            Source = source;
            Previous = previous;
            Next = next;
            _expectedCategory = expectedCategory;
        }


        public override SourceLocation Location
        {
            get
            {
                // If it's missing at the start of file, place it
                // before the next token.
                if (Previous is null)
                    return Source.GetLocation(SourceSpan.EmptyBefore(Next.Span));

                // If the next token is on a new line, report after the previous
                // token. Otherwise, report on the next (offending) token.
                var newlineBetweenPreviousAndNext = Source
                    .GetText(SourceSpan.Between(Previous.Span, Next.Span))
                    .Contains('\n');
                if (newlineBetweenPreviousAndNext)
                    return Source.GetLocation(SourceSpan.EmptyAfter(Previous.Span));

                return Source.GetLocation(Next.Span);
            }
        }

        public override string Message
        {
            get
            {
                var expected = (_expectedKind, _expectedCategory) switch
                {
                    (TokenKind kind, null) => kind.DisplayName,
                    (null, SyntaxCategory.Expr) => "an expression",
                    (null, SyntaxCategory.Stmt) => "a statement",
                    _ => throw new UnreachableException(),
                };

                return $"Expected {expected}.";
            }
        }
    }

    public sealed record InvalidOperatorChaining(SourceFileView Source, Token LeftOperator, Token OperatorToken) : Error
    {
        public override SourceLocation Location => Source.GetLocation(OperatorToken.Span);

        public override string Message =>
            $"Cannot chain {LeftOperator.Kind.DisplayName} and {OperatorToken.Kind.DisplayName}.";

        public override string Hint => "Use parentheses to disambiguate :).";

        public override IReadOnlyList<LabeledSourceLocation> Related =>
        [
            new(Source.GetLocation(LeftOperator.Span),
                "Conflicts with this operator."),
        ];
    }
}