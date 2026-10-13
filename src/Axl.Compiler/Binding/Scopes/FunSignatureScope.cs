using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class FunSignatureScope(Scope enclosingScope, FunSymbol fun)
    : Scope(enclosingScope, owner: fun, flags: BindingFlags.None)
{
    /// <inheritdoc />
    public override ImmutableArray<Symbol> Lookup(string name)
        => [];
}