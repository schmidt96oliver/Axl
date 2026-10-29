using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public closed class StructSymbol(
    string name,
    Symbol owner,
    bool isPublic,
    PrimitiveKind? primitiveKind)
    : TypeSymbol(owner.Compilation, name, owner, isPublic, primitiveKind)
{
    /// <inheritdoc />
    public override bool CanBeReferencedByName => true;

    public ImmutableArray<FieldSymbol> Fields => [.. Members.OfType<FieldSymbol>()];
}