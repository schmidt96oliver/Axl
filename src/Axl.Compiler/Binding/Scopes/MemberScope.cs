using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class MemberScope(Scope enclosingScope, NamespaceOrTypeSymbol namespaceOrType)
    : Scope(enclosingScope, owner: namespaceOrType, flags: BindingFlags.None)
{
    /// <inheritdoc />
    public override ImmutableArray<Symbol> Lookup(string name)
        => namespaceOrType is TypeSymbol type
            ? [.. OwnerAsNamespaceOrType!.LookupMembers(name), .. type.TypeParameters.Where(p => p.Name == name)]
            : OwnerAsNamespaceOrType!.LookupMembers(name);
}