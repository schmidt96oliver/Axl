using System.Collections.Immutable;
using Axl.Compiler.Text;

namespace Axl.Compiler.Taxl;

public abstract record TaxlFragment(SourceFileView SourceView, string Argument)
{
    public sealed record Code(SourceFileView SourceView, string Argument, ImmutableArray<TaxlAnnotation> Annotations) 
        : TaxlFragment(SourceView, Argument);
    
    public sealed record Output(SourceFileView SourceView, string Argument) 
        : TaxlFragment(SourceView, Argument);
}