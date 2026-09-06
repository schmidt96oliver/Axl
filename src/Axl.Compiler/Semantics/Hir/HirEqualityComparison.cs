using Axl.Compiler.Semantics.Types;

namespace Axl.Compiler.Semantics.Hir;

public enum EqualityComparisonKind
{
    Equals,
    NotEquals
}

public sealed class HirEqualityComparison(HirExpr left, HirExpr right, EqualityComparisonKind kind, AxlType type) 
    : HirExpr(type)
{
    public HirExpr Left { get; } = left;
    public HirExpr Right { get; } = right;
    public EqualityComparisonKind Kind { get; } = kind;
}