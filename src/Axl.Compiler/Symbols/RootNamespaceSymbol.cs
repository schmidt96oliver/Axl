using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public class RootNamespaceSymbol : NamespaceSymbol
{
    public RootNamespaceSymbol() 
        : base(name: "", parent: null)
    {
        BaseNamespace = new BaseNamespaceSymbol(parent: this);
    }

    public BaseNamespaceSymbol BaseNamespace { get; }

    public override ImmutableArray<Symbol> Members => [BaseNamespace];
}