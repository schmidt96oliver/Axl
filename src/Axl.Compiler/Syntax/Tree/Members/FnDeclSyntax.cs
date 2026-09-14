using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class FnDeclSyntax(ImmutableArray<SyntaxElement> children)
    : MemberSyntax(SyntaxKind.FnDecl, children)
{
    public IdentifierToken Name => Children.FirstOfType<IdNameSyntax>().Token;

    public IEnumerable<ParamSyntax> Parameters
        => Children.FirstOfType<ParamListSyntax>().Parameters;

    public TypeNameSyntax? ReturnTypeAnnotation
        => Children.FirstOfTypeOrNull<TypeAnnotationClauseSyntax>()?.TypeName;
    
    public FnBodySyntax Body => Children.FirstOfType<FnBodySyntax>();
}
public sealed class FnBodySyntax(ImmutableArray<SyntaxElement> children)
    : MemberSyntax(SyntaxKind.FnBody, children)
{
    public bool IsArm => Children.Any(child => child is Token { Kind: TokenKind.RightDoubleArrow });
    public ExprSyntax? Expr => Children.FirstOfTypeOrNull<ExprSyntax>();
}