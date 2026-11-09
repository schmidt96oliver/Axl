using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class FunBodySyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.FunBody, children)
{
    public bool IsExpressionBodied => Children.Any(child => child is Token { Kind: TokenKind.Equal });
    
    public Token? MutKw => IsExpressionBodied ?
        Children.OfType<Token>().FirstOrDefault(t => t.Kind is TokenKind.MutKw) 
        : null;
    
    public ExprSyntax? Expr => Children.FirstOfTypeOrNull<ExprSyntax>();
}