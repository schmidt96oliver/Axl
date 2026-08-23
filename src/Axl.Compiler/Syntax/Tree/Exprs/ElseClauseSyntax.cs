using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ElseClauseSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.ElseClause, children)
{
    public BodySyntax Body => Children.FirstOfType<BodySyntax>();
}