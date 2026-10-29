namespace Axl.Compiler.Symbols;

public closed class NamespaceSymbol(Compilation compilation, string name, Symbol? owner, bool isRoot)
    : NamespaceOrTypeSymbol(compilation, name, owner)
{
    public bool IsRoot { get; } = isRoot;

    /// <summary>
    /// Namespaces are always public.
    /// </summary>
    public override bool IsPublic => true;
}