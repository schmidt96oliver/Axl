using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;

namespace Axl.Compiler.Semantics.Scopes;

/// <summary>
/// Binder will declare locals, thus this scope is mutable.
/// </summary>
public sealed class LocalScope(Scope? parent) : Scope(parent)
{
    private readonly List<LocalSymbol> _locals = [];

    protected override ImmutableArray<Symbol> LookupOnThisScope(SymbolName name)
        => _locals.LastOrDefault(local => local.Name == name) is { } matchedLocal
            ? [matchedLocal]
            : [];

    public void Declare(LocalSymbol local)
        => _locals.Add(local);
}