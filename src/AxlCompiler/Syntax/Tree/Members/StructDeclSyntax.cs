using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class StructDeclSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.StructDecl, children)
{
    public Token? PubKw
        => Children.OfType<Token>().FirstOrDefault(child => child is { Kind: TokenKind.PubKw });
    
    public IEnumerable<AnnotationSyntax> Annotations => Children.OfType<AnnotationSyntax>();
    
    public TypeParamListSyntax? TypeParameterList => Children.FirstOfTypeOrNull<TypeParamListSyntax>();
    
    public IdentifierToken Name => Children.FirstOfType<IdentifierToken>();
    
    public StructBodySyntax? Body => Children.FirstOfTypeOrNull<StructBodySyntax>();
}