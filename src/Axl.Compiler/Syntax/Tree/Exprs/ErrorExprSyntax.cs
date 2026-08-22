using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ErrorExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.ErrorExpr, children)
{
    public IEnumerable<SyntaxNode> RecoverableNodes
        => Children.OfType<SyntaxNode>().Where(child => child.Kind is not SyntaxKind.Garbage);
}