using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class ConstDeclSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.ConstDecl, children)
{
    public Token? PubKw => Children.FirstNonTriviaToken() is { Kind: TokenKind.PubKw } pubToken
        ? pubToken
        : null;

    public IEnumerable<AnnotationSyntax> Annotations => Children.OfType<AnnotationSyntax>();
    
    public IdentifierToken Name => Children.FirstOfType<IdentifierToken>();

    public ExprSyntax TypeExpr => Children.FirstOfType<TypeAnnotationClauseSyntax>().TypeExpr;
    
    public ExprSyntax ValueExpr => Children.FirstOfType<ExprSyntax>();
}