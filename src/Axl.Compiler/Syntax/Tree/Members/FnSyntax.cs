using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ParamSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.Param, children)
{
    public IdentifierToken Name => Children.FirstOfType<IdNameSyntax>().Token;
    
    public TypeNameSyntax? TypeAnnotation => Children
        .FirstOfKindOrNull(SyntaxKind.TypeAnnotationClause)?
        .Children.FirstOfType<TypeNameSyntax>();
}

public sealed class FnSyntax(ImmutableArray<SyntaxElement> children)
    : MemberSyntax(SyntaxKind.FnDecl, children)
{
    public IEnumerable<Token> Modifiers
        => Children.OfType<Token>().Where(token => token.Kind.IsModifier);

    public StringExprSyntax? NativeName
        => Children.FirstOfKindOrNull(SyntaxKind.NativeClause)?
            .Children.FirstOfType<StringExprSyntax>();
    
    public IdentifierToken Name => Children.FirstOfType<IdNameSyntax>().Token;

    public IEnumerable<ParamSyntax> Parameters
        => Children.FirstOfKind(SyntaxKind.ParamList)
            .Children.OfType<ParamSyntax>();

    public TypeNameSyntax? ReturnTypeAnnotation
        => Children.FirstOfKindOrNull(SyntaxKind.TypeAnnotationClause)?
        .Children.FirstOfType<TypeNameSyntax>();
    
    /// <summary>
    /// Only <c>null</c>, if <see cref="NativeName"/> is not <c>null</c>.
    /// </summary>
    public BodySyntax? Body => Children.FirstOfTypeOrNull<BodySyntax>();
}