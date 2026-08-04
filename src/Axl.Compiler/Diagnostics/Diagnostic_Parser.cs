using Axl.Compiler.Syntax;

namespace Axl.Compiler.Diagnostics;

public abstract partial record Diagnostic
{
    public sealed record UnclosedString(SourceLocation Location) : Error
    {
        public override SourceLocation Location { get; } = Location;
        public override string Message => "String must be closed.";
    }

    public sealed record UnexpectedToken(SourceFileView Source, Token Actual, TokenKind Expected) : Error
    {
        public override SourceLocation Location => Source.GetLocation(Actual.Span);

        public override string Message => $"Expected token '{Expected}', got '{Source.GetText(Actual.Span)}'.";
    }
    
    public sealed record ExpectedStmt(SourceFileView Source, Token Actual) : Error
    {
        public override SourceLocation Location => Source.GetLocation(Actual.Span);

        public override string Message => $"Expected statement at '{Source.GetText(Actual.Span)}'.";
    }
    
    public sealed record ExpectedExpr(SourceFileView Source, Token Actual) : Error
    {
        public override SourceLocation Location => Source.GetLocation(Actual.Span);

        public override string Message => $"Expected expression at '{Source.GetText(Actual.Span)}'.";
    }
}