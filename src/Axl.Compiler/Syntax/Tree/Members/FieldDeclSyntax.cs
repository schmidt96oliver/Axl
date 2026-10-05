using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class FieldDeclSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.FieldDecl, children)
{
    public Token? PubKw => Children.FirstNonTriviaToken() is { Kind: TokenKind.PubKw } pubToken
        ? pubToken
        : null;
    
    public IdNameSyntax Name => Children.FirstOfType<IdNameSyntax>();

    public ExprSyntax TypeExpr => Children.FirstOfType<TypeAnnotationClauseSyntax>().TypeExpr;
}