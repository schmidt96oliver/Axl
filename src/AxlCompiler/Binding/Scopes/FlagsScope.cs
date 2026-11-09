using System.Collections.Immutable;
using AxlCompiler.Symbols;

namespace AxlCompiler.Binding.Scopes;

public sealed class FlagsScope(Scope enclosingScope, BindingFlags flags)
    : Scope(enclosingScope, flags: flags)
{
    /// <inheritdoc />
    public override ImmutableArray<Symbol> MakeVisibleSymbols()
        => [];
}