using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ElseClauseSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.ElseClause, children)
{
    public ExprSyntax Body => Children.FirstOfType<ExprSyntax>();
}