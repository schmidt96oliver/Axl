using System.Collections.Immutable;
using AxlCompiler.Syntax;

namespace AxlCompiler.Symbols;

/// <summary>
/// Represents a reference (i.e. a pointer) to an inner type. Used only by
/// MIR.
/// </summary>
public sealed class RefTypeSymbol(TypeSymbol inner) 
    : TypeSymbol(inner.Compilation, $"ref {inner.Name}", null, false)
{
    public TypeSymbol Inner { get; } = inner;

    /// <inheritdoc />
    public override bool IsConcrete => Inner.IsConcrete;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => [];

    /// <inheritdoc />
    public override ImmutableArray<Token> NameSyntax => [];

    /// <inheritdoc />
    public override ImmutableArray<Symbol> Members => [];

    /// <inheritdoc />
    public override ImmutableArray<TypeParameterSymbol> TypeParameters => [];

    /// <inheritdoc />
    public override ImmutableArray<TypeSymbol> TypeArguments => [];

    /// <inheritdoc />
    public override TypeSymbol Substitute(TypeMap map)
    {
        var substitutedInner = Inner.Substitute(map);
        return substitutedInner != Inner ? new RefTypeSymbol(substitutedInner) : this;
    }
    
    /// <inheritdoc />
    public override bool Equals(Symbol? other)
        // Ref types with the same inner type are equal.
        => other is RefTypeSymbol otherRef &&
           otherRef.Inner.Equals(Inner);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(Inner.GetHashCode(), 0x5245_4600);  // any fixed tag to mean RefTypeSymbol
}