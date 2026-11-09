using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class MutExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.MutExpr, children)
{
    public Token MutKw => Children.FirstNonTriviaToken();

    public ExprSyntax Expr => Children.FirstOfType<ExprSyntax>();
}