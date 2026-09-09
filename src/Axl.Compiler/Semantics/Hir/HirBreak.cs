using System.Collections.Immutable;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirBreak(HirExpr? expr, AxlType type, SyntaxNode syntax) : HirExpr(type, syntax)
{
    public HirExpr? Expr { get; } = expr;

    protected override ImmutableArray<HirStmt> GetChildren() 
        => Expr is not null ? [Expr] : [];

}