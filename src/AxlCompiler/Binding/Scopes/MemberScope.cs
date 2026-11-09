using System.Collections.Immutable;
using AxlCompiler.Symbols;

namespace AxlCompiler.Binding.Scopes;

public sealed class MemberScope(Scope enclosingScope, Symbol symbol)
    : Scope(enclosingScope, owner: symbol, flags: BindingFlags.None)
{
    /// <inheritdoc />
    public override ImmutableArray<Symbol> MakeVisibleSymbols()
        => Owner is TypeSymbol type
            ? [.. Owner.Members, .. type.TypeParameters]
            : Owner.Members;
}