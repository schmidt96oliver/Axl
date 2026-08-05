using System.Diagnostics;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Diagnostics;

public abstract partial record Diagnostic
{
    public sealed record UnclosedString(SourceLocation Location) : Error
    {
        public override SourceLocation Location { get; } = Location;
        public override string Message => "String must be closed.";
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
                    (TokenKind kind, null) => $"'{kind}'",
                    (null, SyntaxCategory.Expr) => "an expression",
                    (null, SyntaxCategory.Stmt) => "a statement",
                    _ => throw new UnreachableException(),
                };

                return $"Expected {expected}, got '{Source.GetText(Actual.Span)}'.";
            }
        }
    }

    public sealed record AmbiguousPrecedence(SourceFileView Source, Token OperatorToken) : Error
    {
        public override SourceLocation Location => Source.GetLocation(OperatorToken.Span);
        public override string Message => $"Precedence is ambiguous at '{Source.GetText(OperatorToken.Span)}'";
        public override string? Hint => "Use parentheses to disambiguate :).";
    }
}