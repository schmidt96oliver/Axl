using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundErrorExpr(ImmutableArray<BoundExpr> recoveredExprs, SyntaxNode? syntax = null) 
    : BoundExpr(ErrorTypeSymbol.Instance, syntax)
{
    public ImmutableArray<BoundExpr> RecoveredExprs { get; } = recoveredExprs;

    protected override ImmutableArray<BoundStmt> GetChildren() => RecoveredExprs.CastArray<BoundStmt>();

}