using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class TypeBodyScope : Scope
{
    public TypeBodyScope(Scope enclosingScope) 
        : base(enclosingScope, flags: BindingFlags.None)
    {
        Guard.IsState(Owner is TypeSymbol);
    }

    /// <inheritdoc />
    public override ImmutableArray<Symbol> Lookup(string name)
        => OwnerAsType!.LookupMembers(name);
}