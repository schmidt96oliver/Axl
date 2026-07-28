namespace Axl.Compiler.Diagnostics;

public abstract partial record Diagnostic
{
    public sealed record StringNotClosed(SourceLocation Location) : Error
    {
        public override SourceLocation Location { get; } = Location;
        public override string Message => "String must be closed.";
    }

    public sealed record InvalidCharacters(SourceLocation Location) : Error
    {
        public override SourceLocation Location { get; } = Location;
        public override string Message => "Invalid characters.";
    }
}