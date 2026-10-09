using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding;

public sealed class Scope
{
    public Scope? Parent { get; }

    /// <summary>
    /// All symbols that have been declared in this scope.
    /// </summary>
    public ImmutableArray<Symbol> VisibleHere { get; }


    private Scope(Scope? parent, ImmutableArray<Symbol> visibleHere)
    {
        Parent = parent;
        VisibleHere = visibleHere;
    }

    public static Scope Root(params IEnumerable<Symbol> visibleSymbols)
        => new Scope(null, [.. visibleSymbols]);
    
    public Scope Nested(params IEnumerable<Symbol> visibleSymbols)
        => new Scope(this, [.. visibleSymbols]);
    
    public Scope Adjacent(params IEnumerable<Symbol> visibleSymbols)
        => new Scope(Parent, [..VisibleHere, .. visibleSymbols]);
    
    /// <summary>
    /// Returns all symbols visible inside only this scope that
    /// match the specified <paramref name="name"/>. First result
    /// is the closest result.
    /// </summary>
    public ImmutableArray<Symbol> LookupHere(string name)
    {
        // Variables can be shadowed inside the same scope,
        // so only the last variable with that name should
        // be added to the result list.
        
        var result = ImmutableArray.CreateBuilder<Symbol>();
        foreach (var symbol in VisibleHere.Reverse().Where(s => s.Name == name))
        {
            if (symbol is VariableSymbol)
            {
                var hasVariableWithSameName = result
                    .OfType<VariableSymbol>()
                    .Any(s => s.Name == symbol.Name);
                if (!hasVariableWithSameName)
                    result.Add(symbol);
            }
            else
                result.Add(symbol);
        }
        
        return result.DrainToImmutable();
    }

    /// <summary>
    /// Returns all symbols visible from this scope (including
    /// symbols visible in parent scopes) matching the
    /// specified <paramref name="name"/>. The first result
    /// is the closest to this scope.
    /// </summary>
    public ImmutableArray<Symbol> Lookup(string name)
        => Parent is not null
            ? [.. LookupHere(name), .. Parent.Lookup(name)]
            : LookupHere(name);
}