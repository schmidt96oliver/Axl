using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundLogicOr(BoundValue left, BoundValue right, TypeSymbol type, SyntaxNode? syntax = null) : BoundValue(type, syntax)
{
    public BoundValue Left { get; } = left;
    public BoundValue Right { get; } = right;

    public override IEnumerable<BoundStmt> GetChildren()
        => [Left, Right];
}