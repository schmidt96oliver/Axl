using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ReturnExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.ReturnExpr, children)
{
    public ExprSyntax? Expr => Children.FirstOfTypeOrNull<ExprSyntax>();
}