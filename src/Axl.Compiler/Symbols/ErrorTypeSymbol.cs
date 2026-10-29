using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class ErrorTypeSymbol(Compilation compilation) 
    : TypeSymbol(compilation, name: "???", owner: null, isPublic: true)
{
    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;
    
    public override ImmutableArray<Symbol> Members => [];
}