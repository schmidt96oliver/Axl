using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class BinaryExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.BinaryExpr, children)
{
    public ExprSyntax Left => Children.FirstOfType<ExprSyntax>();

    public Token Operator => Children.FirstNonTriviaToken();

    public ExprSyntax Right => Children.SecondOfType<ExprSyntax>();
}