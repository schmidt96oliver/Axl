using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class UnaryExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.UnaryExpr, children)
{
    public Token Operator => NthToken(0);
    public ExprSyntax Operand => NthChildOfType<ExprSyntax>(0);
}