using System.Collections.Immutable;
using AxlCompiler.Symbols;

namespace AxlCompiler.Binding.Scopes;

public sealed class GlobalScope(Compilation compilation)
    : Scope(enclosingScope: null, owner: null!, BindingFlags.None)
{
    // Global scope creation triggers root namespace creation,
    // which trigger glocal scope creation. That's why we pass null
    // as owner initially and evaluate it here.
    /// <inheritdoc />
    public override Symbol Owner => compilation.RootNamespace;

    /// <inheritdoc />
    public override ImmutableArray<Symbol> MakeVisibleSymbols()
        // Everything on Base is implicitly visible.
        =>
        [
            .. Compilation.RootNamespace.Members,
            .. Compilation.ReferencedCompilations.SelectMany(refCompilation =>
                refCompilation.RootNamespace.Members),
            .. Compilation.BaseLibrary.BaseNamespace.Members
        ];
}