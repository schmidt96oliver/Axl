using System.Collections.Immutable;
using Axl.Compiler.Text;

namespace Axl.Compiler.Diagnostics;

public abstract partial record Diagnostic
{
    public sealed record UnknownTaxlDirective(SourceLocation Location) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Location];
        public override string Message => $"Directive '{Location.Text.Trim()}' is not known.";
    }

    public sealed record MissingTaxlDirective(SourceLocation Location) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Location];
        public override string Message => $"Test directive missing.";
    }

    public sealed record InvalidTaxlAnnotation(SourceLocation Location) : Error
    {
        public override ImmutableArray<SourceLocation> Locations => [Location];
        public override string Message => $"Annotation '{Location.Text.Trim()}' is invalid.";
    }
}