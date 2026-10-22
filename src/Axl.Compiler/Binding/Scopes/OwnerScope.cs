using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class OwnerScope(Scope enclosingScope, Symbol owner) 
    : Scope(enclosingScope, owner: owner, flags: BindingFlags.None)
{
    /// <inheritdoc />
    public override ImmutableArray<Symbol> Lookup(string name)
        => [];
}