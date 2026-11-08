using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class MemberScope(Scope enclosingScope, Symbol symbol)
    : Scope(enclosingScope, owner: symbol, flags: BindingFlags.None)
{
    /// <inheritdoc />
    public override ImmutableArray<Symbol> MakeVisibleSymbols()
        => Owner is TypeSymbol type
            ? [.. Owner.Members, .. type.TypeParameters]
            : Owner.Members;
}