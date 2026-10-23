namespace Axl.Compiler.Symbols;

public closed class NamespaceSymbol(string name, Symbol? owner, bool isRoot)
    : NamespaceOrTypeSymbol(name, owner)
{
    public bool IsRoot { get; } = isRoot;

    /// <summary>
    /// Namespaces are always public.
    /// </summary>
    public override bool IsPublic => true;
}