using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public class NamespaceSymbol(string name) : NamespaceOrTypeSymbol(name)
{
    public override ImmutableArray<Symbol> Members => [];
}