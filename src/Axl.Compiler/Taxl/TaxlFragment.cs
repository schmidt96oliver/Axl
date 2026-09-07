using System.Collections.Immutable;

namespace Axl.Compiler.Taxl;

public abstract record TaxlFragment(SourceFileView View, string Name)
{
    public sealed record Code(SourceFileView View, string Name, ImmutableArray<TaxlAnnotation> Annotations) 
        : TaxlFragment(View, Name);
    public sealed record Output(SourceFileView View, string Name) 
        : TaxlFragment(View, Name);
}