namespace Axl.Compiler.Symbols;

public closed class Symbol(string name, Symbol? parent)
{
    public string Name { get; } = name;
    
    public Symbol? Parent { get; } = parent;

    public abstract bool IsPublic { get; }

    /// <summary>
    /// The greatest ancestor, where this symbol is still
    /// visible to all its members.
    /// </summary>
    public Symbol Scope => IsPublic
        ? Parent?.Scope ?? this
        : Parent ?? this;

    public IEnumerable<Symbol> AncestorsAndSelf()
    {
        for (var current = this; current is not null; current = current.Parent)
            yield return current;
    }
}