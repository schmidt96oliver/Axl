using Axl.Compiler.Text;

namespace Axl.Compiler.Taxl;

public enum TaxlDirectiveKind
{
    Check,
    RunPass,
    RunPanic,
    Unknown
}

public sealed record TaxlDirective(TaxlDirectiveKind Kind, SourceSpan Span);