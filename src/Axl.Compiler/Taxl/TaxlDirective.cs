namespace Axl.Compiler.Taxl;

public enum TaxlDirectiveKind
{
    Check,
    RunPass,
    RunPanic,
    Unknown
}

public sealed record TaxlDirective(SourceSpan Span, TaxlDirectiveKind Kind);