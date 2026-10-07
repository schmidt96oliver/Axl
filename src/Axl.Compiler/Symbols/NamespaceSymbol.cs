namespace Axl.Compiler.Symbols;

public closed class NamespaceSymbol(string name, Symbol? parent)
    : NamespaceOrTypeSymbol(name, parent)
{
    /// <summary>
    /// Namespaces are always public.
    /// </summary>
    public override bool IsPublic => true;
}