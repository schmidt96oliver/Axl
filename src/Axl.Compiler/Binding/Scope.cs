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
    /// Returns the last symbol with the specified name that
    /// is declared on this scope. Or <c>null</c>, if <paramref name="name"/>
    /// is not declared.
    /// </summary>
    public Symbol? LookupHere(string name)
    {
        // Last declared symbol is first, since it might shadow
        // symbols declared before.
        
        return VisibleHere.LastOrDefault(symbol => symbol.Name.SequenceEqual(name));
    }
    
    /// <summary>
    /// Returns the last symbol with the specified name that
    /// is declared on this or any parent scope.
    /// Or <c>null</c>, if <paramref name="name"/> is not declared.
    /// </summary>
    public Symbol? Lookup(string name)
        => LookupHere(name) ?? Parent?.Lookup(name);

    
}