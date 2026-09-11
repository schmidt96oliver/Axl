using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundAnd(BoundExpr left, BoundExpr right, TypeSymbol type, SyntaxNode syntax) : BoundExpr(type, syntax)
{
    public BoundExpr Left { get; } = left;
    public BoundExpr Right { get; } = right;

    protected override ImmutableArray<BoundStmt> GetChildren() => [Left, Right];
}