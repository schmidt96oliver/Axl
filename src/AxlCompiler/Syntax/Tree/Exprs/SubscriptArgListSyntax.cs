using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class SubscriptArgListSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.SubscriptArgList, children)
{
    public IEnumerable<ExprSyntax> Arguments 
        => Children.OfType<ExprSyntax>();
}