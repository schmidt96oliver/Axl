using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class SubscriptExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.SubscriptExpr, children)
{
    public ExprSyntax Left => Children.FirstOfType<ExprSyntax>();

    public SubscriptArgListSyntax ArgList => Children.FirstOfType<SubscriptArgListSyntax>();
}