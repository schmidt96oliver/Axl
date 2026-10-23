using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public closed class StructSymbol : TypeSymbol
{
    protected StructSymbol(string name,
        Symbol? owner,
        bool isPublic,
        bool isPrimitive) : base(name, owner, isPublic)
    {
        IsPrimitive = isPrimitive;
    }

    /// <summary>
    /// Whether this structs memory representation is internal.
    /// </summary>
    public bool IsPrimitive { get; }

    /// <inheritdoc />
    public override bool CanBeReferencedByName => true;

    public ImmutableArray<FieldSymbol> Fields => [.. Members.OfType<FieldSymbol>()];
}