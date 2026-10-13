using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class FlagsScope(Scope enclosingScope, BindingFlags flags)
    : Scope(enclosingScope, flags: flags)
{
    /// <inheritdoc />
    public override ImmutableArray<Symbol> Lookup(string name)
        => [];
}