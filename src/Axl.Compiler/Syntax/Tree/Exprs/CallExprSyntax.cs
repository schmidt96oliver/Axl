using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class CallExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.CallExpr, children)
{
    public ExprSyntax Callee => NthChildOfType<ExprSyntax>(0);

    public IEnumerable<ExprSyntax> Arguments => NthNodeOfKind(SyntaxKind.ArgList, 0)
        .NodesOfKind(SyntaxKind.Arg)
        .Select(node => node.NthChildOfType<ExprSyntax>(0));
}