namespace Axl.Compiler.Symbols;

public closed class TypeSymbol(string name, Symbol? parent, bool isPublic)
    : NamespaceOrTypeSymbol(name, parent)
{
    public override bool IsPublic { get; } = isPublic;
}