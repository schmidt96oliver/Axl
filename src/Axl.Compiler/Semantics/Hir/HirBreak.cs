using Axl.Compiler.Semantics.Types;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirBreak(HirExpr? expr, AxlType type) : HirExpr(type)
{
    public HirExpr? Expr { get; } = expr;
}