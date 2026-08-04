namespace Axl.Compiler.Diagnostics;

public abstract partial record Diagnostic
{
    public sealed record UnknownCharacters(SourceLocation Location) : Error
    {
        public override SourceLocation Location { get; } = Location;
        public override string Message => $"Unknown character{MessageCharacterSuffix} '{Location.GetText()}'.";

        private string MessageCharacterSuffix => Location.Span.Length != 1 ? "s" : "";
    }

    public sealed record UnknownNumberSuffix(SourceLocation Location) : Error
    {
        public override SourceLocation Location { get; } = Location;
        public override string Message => $"Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got '{Location.GetText()}'.";
    }
    
    public sealed record UnknownEscapeSequence(SourceLocation Location) : Error
    {
        public override SourceLocation Location { get; } = Location;
        public override string Message => $"Unknown escape sequence '{Location.GetText()}'.";
    }
}