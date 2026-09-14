using System.Collections.Immutable;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public class BoundErrorStmt(ImmutableArray<BoundExpr> recoveredExprs, SyntaxNode syntax) : BoundStmt(syntax)
{
    public ImmutableArray<BoundExpr> RecoveredExprs { get; } = recoveredExprs;

    protected override ImmutableArray<BoundStmt> GetChildren() => RecoveredExprs.CastArray<BoundStmt>();
}