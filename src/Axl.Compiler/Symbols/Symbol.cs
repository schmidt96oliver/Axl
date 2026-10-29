using Axl.Compiler.Binding;

namespace Axl.Compiler.Symbols;

public closed class Symbol(Compilation compilation, string name, Symbol? owner)
{
    public Compilation Compilation { get; } = compilation;

    public string Name { get; } = name;

    public Symbol? Owner { get; } = owner;

    public abstract bool IsPublic { get; }

    /// <summary>
    /// Whether this symbols name can be written in source code.
    /// </summary>
    public abstract bool CanBeReferencedByName { get; }

    /// <summary>
    /// The greatest owner where this symbol is still
    /// accessible to all its members.
    /// <c>null</c> means, that this symbol is globally
    /// visible.
    /// </summary>
    public Symbol? AccessibleWithin => IsPublic
        ? Owner?.AccessibleWithin
        // A private global symbol is not globally visible.
        : Owner ?? this;

    public TypeSymbol? OwningType 
        => SelfAndOwners().OfType<TypeSymbol>().FirstOrDefault();

    /// <summary>
    /// Whether this symbol can be accessed from a scope inside
    /// <paramref name="symbol"/>.
    /// </summary>
    /// <param name="symbol"><c>null</c> means global scope.</param>
    public bool IsAccessibleFrom(Symbol? symbol)
        => AccessibleWithin is null || symbol?.SelfAndOwners().Contains(AccessibleWithin) == true;
    
    
    /// <summary>
    /// Completes binding of all lazy symbols and transfers side effects
    /// to <paramref name="sideEffects"/>. Completes any members or
    /// owned symbols as well.
    /// </summary>
    public virtual void Complete(BinderSideEffects sideEffects)
    {

    }

    /// <summary>
    /// Iterates through all owners starting with this symbol.
    /// </summary>
    public IEnumerable<Symbol> SelfAndOwners()
    {
        for (var current = this; current is not null; current = current.Owner)
            yield return current;
    }
}