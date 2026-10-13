using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class TypeHeaderScope(Scope enclosingScope, TypeSymbol type)
    : Scope(enclosingScope, owner: type, flags: BindingFlags.None)
{
    /// <inheritdoc />
    public override ImmutableArray<Symbol> Lookup(string name)
        => [];
}