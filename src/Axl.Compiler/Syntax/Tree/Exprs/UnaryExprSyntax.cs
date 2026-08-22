using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class UnaryExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.UnaryExpr, children)
{
    public Token Operator => Children.FirstNonTriviaToken();
    public ExprSyntax Operand => Children.FirstOfType<ExprSyntax>();
}