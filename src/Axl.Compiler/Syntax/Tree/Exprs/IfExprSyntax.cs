using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class IfExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.IfExpr, children)
{
    public ExprSyntax Predicate => NthChildOfType<ExprSyntax>(0);

    public BodySyntax Body => NthChildOfType<BodySyntax>(0);

    public ExprSyntax? ElseBody => 
        NthNodeOfKindOrNull(SyntaxKind.ElseClause, 0)?
            .NthChildOfType<ExprSyntax>(0);
}