using System.Collections.Immutable;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirAnd(HirExpr left, HirExpr right, AxlType type, SyntaxNode syntax) : HirExpr(type, syntax)
{
    public HirExpr Left { get; } = left;
    public HirExpr Right { get; } = right;

    protected override ImmutableArray<HirStmt> GetChildren() => [Left, Right];
}