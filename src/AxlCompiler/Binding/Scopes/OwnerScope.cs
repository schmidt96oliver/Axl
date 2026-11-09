using System.Collections.Immutable;
using AxlCompiler.Symbols;

namespace AxlCompiler.Binding.Scopes;

public sealed class OwnerScope(Scope enclosingScope, Symbol owner) 
    : Scope(enclosingScope, owner: owner, flags: BindingFlags.None)
{
    /// <inheritdoc />
    public override ImmutableArray<Symbol> MakeVisibleSymbols()
        => [];
}