using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class ErrorExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.ErrorExpr, children)
{
    public IEnumerable<ExprSyntax> RecoverableNodes
        => Children.OfType<ExprSyntax>();
}