using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class ForStmtSyntax(ImmutableArray<SyntaxElement> children)
    : StmtSyntax(SyntaxKind.ForStmt, children)
{
    public IdentifierToken IterationVariableName => Children.FirstOfType<IdentifierToken>();
    
    public ExprSyntax IterableExpr => Children.FirstOfType<ExprSyntax>();

    public ExprSyntax Body => Children.SecondOfType<ExprSyntax>();
}