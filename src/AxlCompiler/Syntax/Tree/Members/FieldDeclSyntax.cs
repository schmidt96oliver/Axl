using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class FieldDeclSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.FieldDecl, children)
{
    public Token? PubKw => Children.FirstNonTriviaToken() is { Kind: TokenKind.PubKw } pubToken
        ? pubToken
        : null;
    
    public NameRefSyntax NameRef => Children.FirstOfType<NameRefSyntax>();

    public ExprSyntax TypeExpr => Children.FirstOfType<TypeAnnotationClauseSyntax>().TypeExpr;
}