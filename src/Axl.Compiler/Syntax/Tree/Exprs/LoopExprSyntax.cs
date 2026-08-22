using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class LoopExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.LoopExpr, children)
{
    public BodySyntax Body => NthChildOfType<BodySyntax>(0);
}