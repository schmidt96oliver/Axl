using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class BreakExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.BreakExpr, children)
{
    public ExprSyntax? Expr => NthChildOfTypeOrNull<ExprSyntax>(0);
}