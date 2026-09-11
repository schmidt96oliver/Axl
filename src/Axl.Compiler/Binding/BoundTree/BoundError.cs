using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public class BoundError(ImmutableArray<BoundExpr> recoveredExprs, TypeSymbol type, SyntaxNode syntax) : BoundExpr(type, syntax)
{
    public ImmutableArray<BoundExpr> RecoveredExprs { get; } = recoveredExprs;

    protected override ImmutableArray<BoundStmt> GetChildren() => RecoveredExprs.CastArray<BoundStmt>();

}