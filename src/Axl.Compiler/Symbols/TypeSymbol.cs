namespace Axl.Compiler.Symbols;

public closed class TypeSymbol(Compilation compilation, string name, Symbol? owner, bool isPublic, PrimitiveKind? primitiveKind = null)
    : NamespaceOrTypeSymbol(compilation, name, owner)
{
    public override bool IsPublic { get; } = isPublic;
    
    public PrimitiveKind? PrimitiveKind { get; } = primitiveKind;

    public bool IsPrimitive => PrimitiveKind is not null;
}