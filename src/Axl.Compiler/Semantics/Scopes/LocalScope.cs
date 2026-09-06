using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;

namespace Axl.Compiler.Semantics.Scopes;

/// <summary>
/// Binder will declare locals, thus this scope is mutable.
/// </summary>
public sealed class LocalScope : Scope
{
    private readonly List<LocalSymbol> _locals = [];
    private readonly ImmutableArray<Symbol> _localMembers;

    public LocalScope(ImmutableArray<Symbol> localMembers, Scope? parent) :
        base(parent)
    {
        _localMembers = localMembers;
    }

    protected override ImmutableArray<Symbol> LookupOnThisScope(SymbolName name)
    {
        var maybeLocal = _locals.LastOrDefault(local => local.Name == name);
        if (maybeLocal is not null)
            return [maybeLocal];

        var localMembers = _localMembers
            .Where(localFn => localFn.Name == name)
            .ToImmutableArray();
        if (localMembers.Length > 0)
            return localMembers.CastArray<Symbol>();

        return [];
    }

    public void Declare(LocalSymbol local)
        => _locals.Add(local);
}