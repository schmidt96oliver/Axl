using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class FunDeclSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.FunDecl, children)
{
    public IdentifierToken Name => Children.FirstOfType<IdNameSyntax>().Token;

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