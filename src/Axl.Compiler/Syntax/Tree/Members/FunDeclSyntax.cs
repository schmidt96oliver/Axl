using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class FunDeclSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.FunDecl, children)
{
    public Token? PubKw 
        => Children.OfType<Token>().FirstOrDefault(child => child is { Kind: TokenKind.PubKw });

    public Token? StaticKw 
        => Children.OfType<Token>().FirstOrDefault(child => child is { Kind: TokenKind.StaticKw });

    public Token? MutKw 
        => Children.OfType<Token>().FirstOrDefault(child => child is { Kind: TokenKind.MutKw });
    
    public IEnumerable<AnnotationSyntax> Annotations => Children.OfType<AnnotationSyntax>();
    
    public FunNameSyntax Name => Children.FirstOfType<FunNameSyntax>();

    public TypeParamListSyntax? TypeParameterList => Children.FirstOfTypeOrNull<TypeParamListSyntax>();
    
    public ParamListSyntax ParameterList => Children.FirstOfType<ParamListSyntax>();
    

    public ReturnTypeAnnotationSyntax? ReturnTypeAnnotation
        => Children.FirstOfTypeOrNull<ReturnTypeAnnotationSyntax>();
    
    public FunBodySyntax Body => Children.FirstOfType<FunBodySyntax>();
}