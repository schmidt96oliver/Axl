namespace Axl.Compiler.Symbols;

public closed class Symbol(string name, Symbol? owner)
{
    public string Name { get; } = name;
    
    public Symbol? Owner { get; } = owner;

    public abstract bool IsPublic { get; }

    /// <summary>
    /// The greatest owner where this symbol is still
    /// accessible to all its members.
    /// </summary>
    public Symbol AccessibleWithin => IsPublic
        ? Owner?.AccessibleWithin ?? this
        : Owner ?? this;

    public TypeSymbol? OwningType 
        => SelfAndOwners().OfType<TypeSymbol>().FirstOrDefault();

    /// <summary>
    /// Iterates through all owners starting with this symbol.
    /// </summary>
    public IEnumerable<Symbol> SelfAndOwners()
    {
        for (var current = this; current is not null; current = current.Owner)
            yield return current;
    }
}