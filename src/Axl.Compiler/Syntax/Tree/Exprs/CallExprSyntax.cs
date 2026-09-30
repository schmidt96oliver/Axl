using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class CallExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.CallExpr, children)
{
    public ExprSyntax Callee => Children.FirstOfType<ExprSyntax>();

    public ArgListSyntax ArgList => Children.FirstOfType<ArgListSyntax>();

    public IEnumerable<ExprSyntax> ArgumentExprs
        => ArgList.Arguments.Select(arg => arg.Expr);
}