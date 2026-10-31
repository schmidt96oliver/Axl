using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

/// <summary>
/// Represents a reference (i.e. a pointer) to an inner type. Used only by
/// MIR.
/// </summary>
public sealed class RefTypeSymbol(TypeSymbol inner) : TypeSymbol(inner.Compilation, $"ref {inner.Name}", null, false)
{
    public TypeSymbol Inner { get; } = inner;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;

    /// <inheritdoc />
    public override ImmutableArray<Symbol> Members => [];

    /// <inheritdoc />
    public override bool Equals(Symbol? other)
        => other is RefTypeSymbol otherRef &&
           otherRef.Inner.Equals(Inner);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(Inner.GetHashCode(), 0x5245_4600);  // any fixed tag to mean RefTypeSymbol
}