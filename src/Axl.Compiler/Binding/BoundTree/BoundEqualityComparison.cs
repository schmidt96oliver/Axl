using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public enum EqualityComparisonKind
{
    Equals,
    NotEquals
}

public sealed class BoundEqualityComparison(
    BoundExpr left,
    BoundExpr right,
    EqualityComparisonKind kind,
    TypeSymbol type,
    SyntaxNode syntax) 
    : BoundExpr(type, syntax)
{
    public BoundExpr Left { get; } = left;
    public BoundExpr Right { get; } = right;
    public EqualityComparisonKind Kind { get; } = kind;

    protected override ImmutableArray<BoundStmt> GetChildren() => [Left, Right];

}