using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class AssignStmtSyntax(ImmutableArray<SyntaxElement> children)
    : StmtSyntax(SyntaxKind.AssignStmt, children)
{
    public ExprSyntax Target => Children.FirstOfType<ExprSyntax>();
    public Token Operator => Children.FirstNonTriviaToken();
    public ExprSyntax Value => Children.SecondOfType<ExprSyntax>();
}