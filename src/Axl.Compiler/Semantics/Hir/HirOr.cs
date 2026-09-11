using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirOr(HirExpr left, HirExpr right, TypeSymbol type, SyntaxNode syntax) : HirExpr(type, syntax)
{
    public HirExpr Left { get; } = left;
    public HirExpr Right { get; } = right;
    
    protected override ImmutableArray<HirStmt> GetChildren() => [Left, Right];
    
}