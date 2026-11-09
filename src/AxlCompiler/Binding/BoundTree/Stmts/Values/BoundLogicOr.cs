using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundLogicOr(BoundValue left, BoundValue right, TypeSymbol type, SyntaxNode? syntax = null) : BoundValue(type, syntax)
{
    public BoundValue Left { get; } = left;
    public BoundValue Right { get; } = right;

    public override IEnumerable<BoundStmt> GetChildren()
        => [Left, Right];
}