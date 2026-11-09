using System.Collections.Immutable;
using AxlCompiler.Syntax;

namespace AxlCompiler.Symbols;

public sealed class ErrorTypeSymbol(Compilation compilation) 
    : TypeSymbol(compilation, name: "???", owner: null, isPublic: true)
{
    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;

    /// <inheritdoc />
    public override ImmutableArray<SyntaxNode> DeclaringSyntax => [];
    
    public override ImmutableArray<Symbol> Members => [];

    /// <inheritdoc />
    public override ImmutableArray<TypeParameterSymbol> TypeParameters => [];

    /// <inheritdoc />
    public override ImmutableArray<TypeSymbol> TypeArguments => [];

    /// <inheritdoc />
    public override TypeSymbol Substitute(TypeMap map)
        // Must be overwritten, because otherwise, it causes crashes
        // when an error is tried to be substituted.
        => this;

    /// <inheritdoc />
    public override bool Equals(Symbol? other)
        // All errors are equal.
        => other is ErrorTypeSymbol;

    /// <inheritdoc />
    public override int GetHashCode()
        => 0x4E52_524F; // any fixed value other than 0
}