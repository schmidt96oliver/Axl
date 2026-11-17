using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class StructOrClassDeclSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.StructOrClassDecl, children)
{
    public Token? PubKw
        => Children.OfType<Token>().FirstOrDefault(child => child is { Kind: TokenKind.PubKw });
    
    public Token StructOrClassKw
        => Children.OfType<Token>().First(child => child is { Kind: TokenKind.ClassKw or TokenKind.StructKw });
    
    public IEnumerable<AnnotationSyntax> Annotations => Children.OfType<AnnotationSyntax>();
    
    public TypeParamListSyntax? TypeParameterList => Children.FirstOfTypeOrNull<TypeParamListSyntax>();
    
    public IdentifierToken Name => Children.FirstOfType<IdentifierToken>();
    
    public StructOrClassBodySyntax? Body => Children.FirstOfTypeOrNull<StructOrClassBodySyntax>();
}