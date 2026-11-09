using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class IfExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.IfExpr, children)
{
    public ExprSyntax Condition => Children.FirstOfType<ConditionClauseSyntax>().Expr;

    public ExprSyntax Body => Children.FirstOfType<ExprSyntax>();

    public ExprSyntax? ElseBody 
        => Children.FirstOfTypeOrNull<ElseClauseSyntax>()?.Body;
}