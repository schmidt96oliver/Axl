namespace Axl.Compiler.Symbols;

public closed class TypeSymbol(string name, Symbol? owner, bool isPublic, PrimitiveKind? primitiveKind = null)
    : NamespaceOrTypeSymbol(name, owner)
{
    public override bool IsPublic { get; } = isPublic;
    
    public PrimitiveKind? PrimitiveKind { get; } = primitiveKind;

    public bool IsPrimitive => PrimitiveKind is not null;
}