using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding;

public sealed class Scope(Scope? parent = null)
{
    private readonly List<Symbol> _declaredSymbols = [];
    
    public Scope? Parent { get; } = parent;

    /// <summary>
    /// All symbols that have been declared in this scope.
    /// </summary>
    public ImmutableArray<Symbol> DeclaredHere => [.. _declaredSymbols];


    /// <summary>
    /// Returns the last symbol with the specified name that
    /// is declared on this scope. Or <c>null</c>, if <paramref name="name"/>
    /// is not declared.
    /// </summary>
    public Symbol? LookupHere(string name)
    {
        // Last declared symbol is first, since it might shadow
        // symbols declared before.
        
        return _declaredSymbols.LastOrDefault(symbol => symbol.Name.SequenceEqual(name));
    }
    
    /// <summary>
    /// Returns the last symbol with the specified name that
    /// is declared on this or any parent scope.
    /// Or <c>null</c>, if <paramref name="name"/> is not declared.
    /// </summary>
    public Symbol? Lookup(string name)
        => LookupHere(name) ?? Parent?.Lookup(name);

    public void Declare(params IEnumerable<Symbol> symbol)
    {
        _declaredSymbols.AddRange(symbol);
    }
}