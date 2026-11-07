using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class FunSignatureScope : Scope
{
    public FunSignatureScope(Scope enclosingScope) 
        : base(enclosingScope, flags: BindingFlags.None)
    {
        Guard.MustBe(OwnerAsFun is not null);
    }

    /// <inheritdoc />
    public override ImmutableArray<Symbol> MakeVisibleSymbols()
        => [.. OwnerAsFun!.TypeParameters];
}