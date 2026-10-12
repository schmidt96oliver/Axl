using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class MemberAccessExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.MemberAccessExpr, children)
{
    public ExprSyntax Left => Children.FirstOfType<ExprSyntax>();

    public IdNameSyntax Member => Children
        .AfterToken(TokenKind.Dot)
        .FirstOfType<IdNameSyntax>();
}