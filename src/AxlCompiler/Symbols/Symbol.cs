using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using AxlCompiler.Binding;
using AxlCompiler.Syntax;
using AxlCompiler.Text;

namespace AxlCompiler.Symbols;

public closed class Symbol(Compilation compilation, string name, Symbol? owner)
    : IEquatable<Symbol>
{
    public Compilation Compilation { get; } = compilation;

    public string Name { get; } = name;

    public virtual Symbol? Owner { get; } = owner;

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
    /// All syntax nodes that originally declared this symbol.
    /// Empty for generated symbols. Used for descending into
    /// symbols by location.
    /// </summary>
    public abstract ImmutableArray<SyntaxNode> DeclaringSyntax { get; }

    /// <summary>
    /// All tokens that introduce the name for this symbol.
    /// Empty for generated symbols.
    /// </summary>
    public abstract ImmutableArray<Token> NameSyntax { get; }


    public virtual ImmutableArray<Symbol> Members => [];

    /// <summary>
    /// Returns all members that match the specified <paramref name="name"/>
    /// in no particular order.
    /// </summary>
    public virtual ImmutableArray<Symbol> LookupMembers(string name)
        => [.. Members.Where(s => s.Name == name)];

    /// <summary>
    /// Traverse this symbol and its member in pre-order depth-first order,
    /// Includes this symbol. Only descends into a symbol (including this one),
    /// if <paramref name="descendInto"/> evaluates <c>true</c>. Self is not checked
    /// against <paramref name="descendInto"/>. Note that members
    /// have no inherent order.
    /// </summary>
    public IEnumerable<Symbol> Traverse(Func<Symbol, bool> descendInto)
    {
        var stack = new Stack<Symbol>();
        stack.Push(this);

        while (stack.TryPop(out var current))
        {
            yield return current;

            foreach (var member in current.Members.Where(descendInto))
                stack.Push(member);
        }
    }

    public bool DeclarationContains(SourceLocation location)
        => DeclaringSyntax.Any(syntax => syntax.Location.Contains(location));
    
    /// <summary>
    /// The greatest owner where this symbol is still
    /// accessible to all its members.
    /// <c>null</c> means, that this symbol is globally
    /// visible.
    /// </summary>
    public Symbol? AccessibleWithin => IsPublic
        ? OriginalDefinition.Owner?.AccessibleWithin
        // A private global symbol is not globally visible.
        : OriginalDefinition.Owner ?? this;

    /// <summary>
    /// Whether this symbol can be accessed from a scope inside
    /// <paramref name="symbol"/>.
    /// </summary>
    /// <param name="symbol"><c>null</c> means global scope.</param>
    public bool IsAccessibleFrom(Symbol? symbol)
        => AccessibleWithin is null || symbol?.OriginalDefinition.SelfAndOwners().Contains(AccessibleWithin) == true;

    /// <summary>
    /// Replaces all type parameters within this symbol
    /// through the given <paramref name="map"/>.
    /// </summary>
    public virtual Symbol Substitute(TypeMap map) => this;
    
    /// <summary>
    /// Ensures that lazy symbols are fully bound and analyzed and
    /// transfers side effects to <paramref name="sideEffects"/>. Completes
    /// members as well and can by called multiple times.
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