using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class GlobalScope(Compilation compilation)
    : Scope(enclosingScope: null, owner: null, BindingFlags.None, compilation.BaseNamespace)
{
    public Compilation Compilation { get; } = compilation;

    /// <inheritdoc />
    public override ImmutableArray<Symbol> Lookup(string name)
    {
        // Everything on Base is implicitly visible.

        return
        [
            .. Compilation.ReferencedCompilations.SelectMany(refCompilation =>
                refCompilation.RootNamespace.LookupMembers(name)),
            .. BaseNamespace.LookupMembers(name)
        ];
    }
}