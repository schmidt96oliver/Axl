namespace Axl.Compiler.Symbols;

public closed class TypeSymbol(string name, Symbol? owner, bool isPublic)
    : NamespaceOrTypeSymbol(name, owner)
{
    public override bool IsPublic { get; } = isPublic;
}