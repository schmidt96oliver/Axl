using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class MemberScope(Scope enclosingScope, NamespaceOrTypeSymbol namespaceOrType)
    : Scope(enclosingScope, owner: namespaceOrType, flags: BindingFlags.None)
{
    /// <inheritdoc />
    public override ImmutableArray<Symbol> Lookup(string name)
        => OwnerAsNamespaceOrType!.LookupMembers(name);
}