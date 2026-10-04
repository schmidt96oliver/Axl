using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class UsingDirectiveSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.UsingDirective, children)
{
    public ExprSyntax NameExpr => Children.FirstOfType<ExprSyntax>();
}