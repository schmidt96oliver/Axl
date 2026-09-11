using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public class HirErrorExpr(ImmutableArray<HirExpr> recoveredExprs, TypeSymbol type, SyntaxNode syntax) : HirExpr(type, syntax)
{
    public ImmutableArray<HirExpr> RecoveredExprs { get; } = recoveredExprs;

    protected override ImmutableArray<HirStmt> GetChildren() => RecoveredExprs.CastArray<HirStmt>();

}