namespace Axl.Compiler.Symbols;

public closed class NamespaceSymbol(Compilation compilation, string name, Symbol? owner, bool isRoot)
    : Symbol(compilation, name, owner)
{
    public bool IsRoot { get; } = isRoot;

    /// <summary>
    /// Namespaces are always public.
    /// </summary>
    public override bool IsPublic => true;

    /// <summary>
    /// Namespaces are always concrete.
    /// </summary>
    public override bool IsConcrete => true;
}