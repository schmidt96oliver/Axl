using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;

namespace Axl.Compiler.Semantics.Scopes;

/// <summary>
/// Binder will declare locals, thus this scope is mutable.
/// </summary>
public sealed class LocalScope : Scope
{
    private readonly List<LocalSymbol> _locals = [];
    private readonly ImmutableArray<FnSymbol> _localFns;

    public LocalScope(ImmutableArray<FnSymbol> localFns, Scope? parent) :
        base(parent)
    {
        _localFns = localFns;
    }

    protected override ImmutableArray<Symbol> LookupOnThisScope(SymbolName name)
    {
        var maybeLocal = _locals.LastOrDefault(local => local.Name == name);
        if (maybeLocal is not null)
            return [maybeLocal];

        var localFns = _localFns
            .Where(localFn => localFn.Name == name)
            .ToImmutableArray();
        if (localFns.Length > 0)
            return localFns.CastArray<Symbol>();

        return [];
    }

    public void Declare(LocalSymbol local)
        => _locals.Add(local);
}