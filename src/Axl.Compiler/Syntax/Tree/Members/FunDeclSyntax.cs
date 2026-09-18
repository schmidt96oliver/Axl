using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class FunDeclSyntax(ImmutableArray<SyntaxElement> children)
    : MemberSyntax(SyntaxKind.FunDecl, children)
{
    public IdentifierToken Name => Children.FirstOfType<IdNameSyntax>().Token;

    public IEnumerable<ParamSyntax> Parameters
        => Children.FirstOfType<ParamListSyntax>().Parameters;

    public TypeNameSyntax? ReturnTypeAnnotation
        => Children.FirstOfTypeOrNull<TypeAnnotationClauseSyntax>()?.TypeName;
    
    public FunBodySyntax Body => Children.FirstOfType<FunBodySyntax>();
}
public sealed class FunBodySyntax(ImmutableArray<SyntaxElement> children)
    : MemberSyntax(SyntaxKind.FunBody, children)
{
    public bool IsExpressionBodied => Children.Any(child => child is Token { Kind: TokenKind.Equal });
    public ExprSyntax? Expr => Children.FirstOfTypeOrNull<ExprSyntax>();
}