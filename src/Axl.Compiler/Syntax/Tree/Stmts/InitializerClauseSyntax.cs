using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class InitializerClauseSyntax(ImmutableArray<SyntaxElement> children)
    : StmtSyntax(SyntaxKind.InitializerClause, children)
{
    public ExprSyntax Expr 
        => Children.FirstOfType<ExprSyntax>();
}