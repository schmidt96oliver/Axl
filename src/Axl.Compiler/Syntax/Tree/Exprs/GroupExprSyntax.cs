using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class GroupExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.GroupExpr, children)
{
    public ExprSyntax Inner => NthChildOfType<ExprSyntax>(0);
}