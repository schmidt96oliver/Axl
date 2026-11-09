using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class ReturnExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.ReturnExpr, children)
{
    public Token? MutKw => Children.OfType<Token>().FirstOrDefault(t => t.Kind is TokenKind.MutKw);
    
    public ExprSyntax? Expr => Children.FirstOfTypeOrNull<ExprSyntax>();
}