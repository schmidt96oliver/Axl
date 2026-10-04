using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class TypeAnnotationClauseSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.TypeAnnotationClause, children)
{
    public ExprSyntax TypeExpr => Children.FirstOfType<ExprSyntax>();
}