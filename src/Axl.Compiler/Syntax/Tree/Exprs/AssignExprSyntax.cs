using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class AssignExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.AssignExpr, children)
{
    public ExprSyntax Target => NthChildOfType<ExprSyntax>(0);
    public ExprSyntax Value => NthChildOfType<ExprSyntax>(1);
}