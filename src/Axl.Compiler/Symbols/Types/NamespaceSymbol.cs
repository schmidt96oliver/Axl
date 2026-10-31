using System.Runtime.CompilerServices;

namespace Axl.Compiler.Symbols;

public closed class NamespaceSymbol(Compilation compilation, string name, Symbol? owner, bool isRoot)
    : NamespaceOrTypeSymbol(compilation, name, owner)
{
    public bool IsRoot { get; } = isRoot;

    /// <summary>
    /// Namespaces are always public.
    /// </summary>
    public override bool IsPublic => true;


    /// <inheritdoc />
    public override bool Equals(Symbol? other)
        => ReferenceEquals(this,
            other); // Namespace have no substituted types, so there will only be one reference.

    /// <inheritdoc />
    public override int GetHashCode()
        => RuntimeHelpers.GetHashCode(this);
}