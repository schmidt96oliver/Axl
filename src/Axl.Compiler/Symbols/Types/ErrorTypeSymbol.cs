using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class ErrorTypeSymbol(Compilation compilation) 
    : TypeSymbol(compilation, name: "???", owner: null, isPublic: true)
{
    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;

    public override ImmutableArray<Symbol> Members => [];

    /// <inheritdoc />
    public override bool Equals(Symbol? other)
        => other is ErrorTypeSymbol;

    /// <inheritdoc />
    public override int GetHashCode()
        => 0x4E52_524F; // any fixed value other than 0

    /// <inheritdoc />
    public override TypeSymbol Substitute(TypeMap map)
        => this;
}