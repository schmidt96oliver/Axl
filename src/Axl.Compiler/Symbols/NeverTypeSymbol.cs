using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class NeverTypeSymbol(Compilation compilation) 
    : TypeSymbol(compilation, name: "Never", owner: null, isPublic: true)
{
    public override ImmutableArray<Symbol> Members => [];

    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;
}