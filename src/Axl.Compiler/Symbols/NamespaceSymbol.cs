namespace Axl.Compiler.Symbols;

public closed class NamespaceSymbol(string name, Symbol? owner)
    : NamespaceOrTypeSymbol(name, owner)
{
    /// <summary>
    /// Namespaces are always public.
    /// </summary>
    public override bool IsPublic => true;
}