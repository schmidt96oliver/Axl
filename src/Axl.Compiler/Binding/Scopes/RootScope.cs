using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class RootScope(RootNamespaceSymbol rootNamespace)
    : Scope(null, rootNamespace, BindingFlags.None, rootNamespace.BaseNamespace)
{
    /// <inheritdoc />
    public override ImmutableArray<Symbol> Lookup(string name)
    {
        // Everything on Base is implicitly visible.
        
        return [.. rootNamespace.LookupMembers(name), .. BaseNamespace.LookupMembers(name)];
    }
}