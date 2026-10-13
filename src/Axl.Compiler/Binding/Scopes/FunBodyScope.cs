using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class FunBodyScope : Scope
{
    public FunBodyScope(Scope enclosingScope) 
        : base(enclosingScope, flags: BindingFlags.None)
    {
        Guard.MustBe(OwnerAsFun is not null);
    }

    /// <inheritdoc />
    public override ImmutableArray<Symbol> Lookup(string name)
        => [.. OwnerAsFun!.Parameters.Where(p => p.Name == name)];
}