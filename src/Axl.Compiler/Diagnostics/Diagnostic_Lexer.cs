using System.Collections.Immutable;

namespace Axl.Compiler.Diagnostics;

public abstract partial record Diagnostic
{
    public sealed record UnknownNumberSuffix(SourceLocation Location) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Location];
        public override string Message => $"Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got '{Location.GetText()}'.";
    }

    public sealed record UnknownEscapeSequence(SourceLocation Location) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Location];
        public override string Message => $"Unknown escape sequence '{Location.GetText()}'.";
    }
}