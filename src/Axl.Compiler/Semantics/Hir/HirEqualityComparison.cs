using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public enum EqualityComparisonKind
{
    Equals,
    NotEquals
}

public sealed class HirEqualityComparison(
    HirExpr left,
    HirExpr right,
    EqualityComparisonKind kind,
    AxlType type,
    SyntaxNode syntax) 
    : HirExpr(type, syntax)
{
    public HirExpr Left { get; } = left;
    public HirExpr Right { get; } = right;
    public EqualityComparisonKind Kind { get; } = kind;
}