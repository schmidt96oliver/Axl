using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ArgSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.Arg, children)
{
    // Arg = (IdName "=")? Expr

    public bool IsNamed => Children.Any(el => el is Token { Kind: TokenKind.Equal });
    
    public IdNameSyntax? Name => IsNamed ? Children.FirstOfType<IdNameSyntax>() : null;

    public ExprSyntax Expr => IsNamed ? Children.SecondOfType<ExprSyntax>() : Children.FirstOfType<ExprSyntax>();
}