using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class GlobalScope(Compilation compilation)
    : Scope(enclosingScope: null, owner: null!, BindingFlags.None)
{
    // Global scope creation triggers root namespace creation,
    // which trigger glocal scope creation. That's why we pass null
    // as owner initially and evaluate it here.
    /// <inheritdoc />
    public override Symbol Owner => compilation.RootNamespace;

    /// <inheritdoc />
    public override ImmutableArray<Symbol> Lookup(string name)
    {
        // Everything on Base is implicitly visible.

        return
        [
            .. Compilation.RootNamespace.LookupMembers(name),
            .. Compilation.ReferencedCompilations.SelectMany(refCompilation =>
                refCompilation.RootNamespace.LookupMembers(name)),
            .. Compilation.BaseLibrary.BaseNamespace.LookupMembers(name)
        ];
    }
}