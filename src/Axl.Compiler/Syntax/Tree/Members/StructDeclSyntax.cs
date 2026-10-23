using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class StructDeclSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.StructDecl, children)
{
    public Token? PubKw
        => Children.OfType<Token>().FirstOrDefault(child => child is { Kind: TokenKind.PubKw });
    
    public IEnumerable<AnnotationSyntax> Annotations => Children.OfType<AnnotationSyntax>();
    
    public IdentifierToken Name => Children.FirstOfType<IdNameSyntax>().Token;
    
    public StructBodySyntax? Body => Children.FirstOfTypeOrNull<StructBodySyntax>();
}