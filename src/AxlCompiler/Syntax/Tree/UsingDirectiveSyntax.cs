using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class UsingDirectiveSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.UsingDirective, children)
{
    public ExprSyntax NameExpr => Children.FirstOfType<ExprSyntax>();
}