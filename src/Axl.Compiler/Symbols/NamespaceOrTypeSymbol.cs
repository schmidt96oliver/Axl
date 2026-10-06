using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public closed class NamespaceOrTypeSymbol(string name) : Symbol(name)
{
    public abstract ImmutableArray<Symbol> Members { get; }

    public Symbol? LookupMember(string name)
        => name is ""
            ? null
            : Members.SingleOrDefault(symbol => symbol.Name == name);
}