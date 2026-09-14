using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class WhileStmtSyntax(ImmutableArray<SyntaxElement> children)
    : StmtSyntax(SyntaxKind.WhileStmt, children)
{
    public ExprSyntax Condition => Children.FirstOfType<ConditionClauseSyntax>().Expr;

    public ExprSyntax Body => Children.FirstOfType<ExprSyntax>();
}