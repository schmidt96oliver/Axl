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

    public bool IsOperatorName => Name.Token.Kind is not TokenKind.Identifier;

    public ParamListSyntax ParameterList => Children.FirstOfType<ParamListSyntax>();
    
    public IEnumerable<ParamSyntax> Parameters
        => ParameterList.Parameters;

    public ExprSyntax? ReturnTypeAnnotation
        => Children.FirstOfTypeOrNull<TypeAnnotationClauseSyntax>()?.TypeExpr;
    
    public FunBodySyntax Body => Children.FirstOfType<FunBodySyntax>();
}

public sealed class FunBodySyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.FunBody, children)
{
    public bool IsExpressionBodied => Children.Any(child => child is Token { Kind: TokenKind.Equal });
    public ExprSyntax? Expr => Children.FirstOfTypeOrNull<ExprSyntax>();
}