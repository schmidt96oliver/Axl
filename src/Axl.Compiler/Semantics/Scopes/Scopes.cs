using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;

namespace Axl.Compiler.Semantics.Scopes;

public abstract class Scope(Scope? parent)
{
    public Scope? Parent { get; } = parent;

    /// <summary>
    /// Returns all <see cref="Symbol"/>s on this scope that are validly referenced
    /// under the given <paramref name="name"/>.
    /// </summary>
    protected abstract ImmutableArray<Symbol> LookupOnThisScope(SymbolName name);

    public virtual ImmutableArray<Symbol> Lookup(SymbolName name)
    {
        var thisScope = LookupOnThisScope(name);
        if (!thisScope.IsEmpty)
            return thisScope;

        return Parent?.Lookup(name) ?? [];
    }
}