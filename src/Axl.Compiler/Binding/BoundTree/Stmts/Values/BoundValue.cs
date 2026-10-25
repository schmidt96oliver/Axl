using Axl.Compiler.Lowering;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public closed class BoundValue(TypeSymbol type, SyntaxNode? syntax = null) : BoundStmt(syntax)
{
    public TypeSymbol Type { get; } = type;

    /// <summary>
    /// If this value represents a place, returns
    /// it's description. <c>null</c>, if this value
    /// is not a place.
    /// </summary>
    public virtual Place? Place => null;
}

public readonly record struct Place(BoundValue Root, bool IsMut);