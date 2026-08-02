namespace Axl.Compiler.Diagnostics;

public abstract partial record Diagnostic
{
    public sealed record UnclosedString(SourceLocation Location) : Error
    {
        public override SourceLocation Location { get; } = Location;
        public override string Message => "String must be closed.";
    }

    public sealed record InvalidCharacters(SourceLocation Location) : Error
    {
        public override SourceLocation Location { get; } = Location;
        public override string Message => "Invalid characters.";
    }

    public sealed record UnknownNumberSuffix(SourceLocation Location) : Error
    {
        public override SourceLocation Location { get; } = Location;
        public override string Message => $"Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got '{Location.GetText()}'.";
    }
}