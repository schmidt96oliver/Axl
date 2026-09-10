using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;
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
    TypeSymbol type,
    SyntaxNode syntax) 
    : HirExpr(type, syntax)
{
    public HirExpr Left { get; } = left;
    public HirExpr Right { get; } = right;
    public EqualityComparisonKind Kind { get; } = kind;

    protected override ImmutableArray<HirStmt> GetChildren() => [Left, Right];

}