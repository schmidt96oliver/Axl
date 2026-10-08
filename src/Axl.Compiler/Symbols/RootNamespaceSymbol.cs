using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public class RootNamespaceSymbol : NamespaceSymbol
{
    public RootNamespaceSymbol() 
        : base(name: "", owner: null)
    {
        BaseNamespace = new BaseNamespaceSymbol(owner: this);
    }

    public BaseNamespaceSymbol BaseNamespace { get; }

    public override ImmutableArray<Symbol> Members => [BaseNamespace];
}