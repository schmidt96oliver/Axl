using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public closed class BoundValue(TypeSymbol type, SyntaxNode? syntax = null) : BoundStmt(syntax)
{
    public TypeSymbol Type { get; } = type;

    /// <summary>
    /// Whether this value refers to a storage location.
    /// </summary>
    public virtual bool IsPlace => false;

    /// <summary>
    /// Whether this value refers to a writable storage location.
    /// If <c>true</c>, if implies that <see cref="IsPlace"/> is <c>true</c>.
    /// </summary>
    public virtual bool IsAssignable => false;
}