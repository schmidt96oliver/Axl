using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class MemberAccessExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.MemberAccessExpr, children)
{
    public ExprSyntax Left => Children.FirstOfType<ExprSyntax>();

    public IdNameSyntax Member => Children
        .AfterToken(TokenKind.Dot)
        .FirstOfType<IdNameSyntax>();
}