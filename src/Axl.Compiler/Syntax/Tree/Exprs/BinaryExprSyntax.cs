using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class BinaryExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.BinaryExpr, children)
{
    public ExprSyntax Left => NthChildOfType<ExprSyntax>(0);

    public Token Operator => NthToken(0);

    public ExprSyntax Right => NthChildOfType<ExprSyntax>(1);
}