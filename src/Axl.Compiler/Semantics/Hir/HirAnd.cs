using Axl.Compiler.Semantics.Types;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirAnd(HirExpr left, HirExpr right, AxlType type) : HirExpr(type)
{
    public HirExpr Left { get; } = left;
    public HirExpr Right { get; } = right;
}