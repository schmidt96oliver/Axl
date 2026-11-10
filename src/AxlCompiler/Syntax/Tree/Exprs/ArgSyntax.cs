using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class ArgSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.Arg, children)
{
    // Arg = (NameRef "=")? "mut"? Expr

    public bool IsNamed => Children.Any(el => el is Token { Kind: TokenKind.Equal });
    
    public NameRefSyntax? Name => IsNamed ? Children.FirstOfType<NameRefSyntax>() : null;

    public Token? MutKw => Children.OfType<Token>().FirstOrDefault(el => el.Kind == TokenKind.MutKw);
    
    public ExprSyntax Expr => IsNamed ? Children.SecondOfType<ExprSyntax>() : Children.FirstOfType<ExprSyntax>();
}