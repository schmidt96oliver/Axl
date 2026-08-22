using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ExprStmtSyntax(ImmutableArray<SyntaxElement> children)
    : StmtSyntax(SyntaxKind.ExprStmt, children)
{
    public ExprSyntax Expr => Children.FirstOfType<ExprSyntax>();
}