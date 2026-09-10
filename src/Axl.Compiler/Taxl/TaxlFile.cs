using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;

namespace Axl.Compiler.Taxl;

public sealed class TaxlFile
{
    public SourceFileView Source { get; }
    public ImmutableArray<TaxlFragment> Fragments { get; }
    public ImmutableArray<TaxlDirective> Directives { get; }


    private TaxlFile(SourceFileView source, ImmutableArray<TaxlFragment> fragments, ImmutableArray<TaxlDirective> directives)
    {
        Source = source;
        Fragments = fragments;
        Directives = directives;
    }


    public static TaxlFile From(SourceFileView source)
    {
        var parser = new TaxlParser(source);
        
        var fragments = parser.ParseFragments();
        var directives = parser.ParseDirectives(fragments[0]);
        return new TaxlFile(source, fragments, directives);
    }

}