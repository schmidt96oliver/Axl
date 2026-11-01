using System.Runtime.CompilerServices;
using Axl.Compiler.Binding;

namespace Axl.Compiler.Symbols;

public closed class Symbol(Compilation compilation, string name, Symbol? owner)
    : IEquatable<Symbol>
{
    public Compilation Compilation { get; } = compilation;

    public string Name { get; } = name;

    public Symbol? Owner { get; } = owner;

    public TypeSymbol? OwningType
        => SelfAndOwners().OfType<TypeSymbol>().FirstOrDefault();


    /// <summary>
    /// If it is generic, this
    /// symbol might be specialized, so <see cref="OriginalDefinition"/>
    /// refers to the symbol as originally defined in source code.
    /// </summary>
    public virtual Symbol OriginalDefinition => this;

    public abstract bool IsPublic { get; }

    /// <summary>
    /// <c>false</c>, if this symbol has unfilled type parameters or if
    /// they are filled with generic arguments. Intuitively, <c>false</c>,
    /// if it is generic. <c>true</c>, if all type parameters are substituted
    /// by concrete types.
    /// </summary>
    public abstract bool IsConcrete { get; }

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

    /// <summary>
    /// Whether this symbol can be accessed from a scope inside
    /// <paramref name="symbol"/>.
    /// </summary>
    /// <param name="symbol"><c>null</c> means global scope.</param>
    public bool IsAccessibleFrom(Symbol? symbol)
        => AccessibleWithin is null || symbol?.SelfAndOwners().Contains(AccessibleWithin) == true;

    /// <summary>
    /// Replaces all type parameters within this symbol
    /// through the given <paramref name="map"/>.
    /// </summary>
    public virtual Symbol Substitute(TypeMap map) => this;
    
    /// <summary>
    /// Completes binding of all lazy symbols and transfers side effects
    /// to <paramref name="sideEffects"/>. Completes any members or
    /// owned symbols as well.
    /// </summary>
    public virtual void Complete(BinderSideEffects sideEffects)
    {

    }

    /// <summary>
    /// Iterates through all owners starting with the owner of this symbol.
    /// </summary>
    public IEnumerable<Symbol> Owners()
    {
        for (var current = this.Owner; current is not null; current = current.Owner)
            yield return current;
    }
    
    /// <summary>
    /// Iterates through all owners starting with this symbol.
    /// </summary>
    public IEnumerable<Symbol> SelfAndOwners()
    {
        for (var current = this; current is not null; current = current.Owner)
            yield return current;
    }

    // !!! Equality needs to be overwritten sometimes, because symbols with 
    //     substituted type parameters could come through different paths.
    //     Most symbols will implement reference equality, if they are source
    //     declared. But some use value equality.

    /// <inheritdoc />
    public virtual bool Equals(Symbol? other)
        => ReferenceEquals(this, other);
    
    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not Symbol otherSymbol) return false;
        return Equals(otherSymbol);
    }

    /// <inheritdoc />
    public override int GetHashCode()
        => RuntimeHelpers.GetHashCode(this);

    public static bool operator ==(Symbol? left, Symbol? right)
        => ReferenceEquals(left, right) || (left is not null && left.Equals(right));

    public static bool operator !=(Symbol? left, Symbol? right)
        => !(left == right);
}