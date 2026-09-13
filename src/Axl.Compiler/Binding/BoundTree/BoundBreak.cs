using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundBreak(BoundExpr? expr, TypeSymbol type, SyntaxNode syntax)
    : BoundExpr(type, syntax)
{
    public BoundExpr? Expr { get; } = expr;

    protected override ImmutableArray<BoundStmt> GetChildren()
        => Expr is not null ? [Expr] : [];
}