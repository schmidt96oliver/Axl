using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class ArgSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.Arg, children)
{
    // Arg = (IdName "=")? "mut"? Expr

    public bool IsNamed => Children.Any(el => el is Token { Kind: TokenKind.Equal });
    
    public IdNameSyntax? Name => IsNamed ? Children.FirstOfType<IdNameSyntax>() : null;

    public Token? MutKw => Children.OfType<Token>().FirstOrDefault(el => el.Kind == TokenKind.MutKw);
    
    public ExprSyntax Expr => IsNamed ? Children.SecondOfType<ExprSyntax>() : Children.FirstOfType<ExprSyntax>();
}