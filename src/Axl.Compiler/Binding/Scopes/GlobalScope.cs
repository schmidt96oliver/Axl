using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.Scopes;

public sealed class GlobalScope(Compilation compilation)
    : Scope(null, null!, BindingFlags.None, compilation.BaseNamespace)
{
    /// <inheritdoc />
    public override Symbol Owner =>
        throw new InvalidOperationException(
            $"{nameof(GlobalScope)} has no owner. It cannot be used for a {nameof(Binder)}.");

    /// <inheritdoc />
    public override ImmutableArray<Symbol> Lookup(string name)
    {
        // Everything on Base is implicitly visible.

        return
        [
            .. compilation.ReferencedCompilations.SelectMany(refCompilation =>
                refCompilation.RootNamespace.LookupMembers(name)),
            .. BaseNamespace.LookupMembers(name)
        ];
    }
}