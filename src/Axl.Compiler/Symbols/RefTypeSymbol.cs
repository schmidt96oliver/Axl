using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

/// <summary>
/// Represents a reference (i.e. a pointer) to an inner type. Used only by
/// MIR.
/// </summary>
public sealed class RefTypeSymbol(TypeSymbol inner) 
    : TypeSymbol($"ref {inner.Name}", null, false)
{
    public TypeSymbol Inner { get; } = inner;

    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;

    /// <inheritdoc />
    public override ImmutableArray<Symbol> Members => [];
}