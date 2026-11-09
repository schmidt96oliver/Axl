using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class ReturnExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.ReturnExpr, children)
{
    public ExprSyntax? Expr => Children.FirstOfTypeOrNull<ExprSyntax>();
}