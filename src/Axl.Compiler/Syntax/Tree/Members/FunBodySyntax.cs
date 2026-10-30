using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class FunBodySyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.FunBody, children)
{
    public bool IsExpressionBodied => Children.Any(child => child is Token { Kind: TokenKind.Equal });
    public ExprSyntax? Expr => Children.FirstOfTypeOrNull<ExprSyntax>();
}