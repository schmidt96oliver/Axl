using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ErrorExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.ErrorExpr, children)
{
    public IEnumerable<ExprSyntax> RecoverableNodes
        => Children.OfType<ExprSyntax>();
}