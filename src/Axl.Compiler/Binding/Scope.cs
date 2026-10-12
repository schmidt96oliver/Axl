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
    
    public ImmutableArray<Symbol> Lookup(string name)
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

}