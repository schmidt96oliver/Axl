using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public closed class NamespaceOrTypeSymbol(string name, Symbol? owner) 
    : Symbol(name, owner)
{
    public abstract ImmutableArray<Symbol> Members { get; }

    /// <summary>
    /// Returns all members that match the specified <paramref name="name"/>
    /// in no particular order.
    /// </summary>
    public ImmutableArray<Symbol> LookupMembers(string name)
        => [.. Members.Where(s => s.Name == name)];
}