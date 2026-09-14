using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ConditionClauseSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.ConditionClause, children)
{
    public ExprSyntax Expr => Children.FirstOfType<ExprSyntax>();
}