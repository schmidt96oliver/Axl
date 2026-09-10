using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirAnd(HirExpr left, HirExpr right, TypeSymbol type, SyntaxNode syntax) : HirExpr(type, syntax)
{
    public HirExpr Left { get; } = left;
    public HirExpr Right { get; } = right;

    protected override ImmutableArray<HirStmt> GetChildren() => [Left, Right];
}