using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class BinaryExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.BinaryExpr, children)
{
    public ExprSyntax Left => Children.FirstOfType<ExprSyntax>();

    public Token Operator => Children.FirstNonTriviaToken();

    public ExprSyntax Right => Children.SecondOfType<ExprSyntax>();
}