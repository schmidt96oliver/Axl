using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class GetMemberExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.GetMemberExpr, children)
{
    public ExprSyntax Left => NthChildOfType<ExprSyntax>(0);

    public IdNameSyntax Member => ChildrenAfter(TokenKind.Dot)
        .OfType<IdNameSyntax>()
        .First();
}