using Axl.Compiler.Semantics.Types;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirIf(HirExpr predicate, HirExpr then, HirExpr? @else, AxlType type)
    : HirExpr(type)
{
    public HirExpr Predicate { get; } = predicate;
    public HirExpr Then { get; } = then;
    public HirExpr? Else { get; } = @else;
}