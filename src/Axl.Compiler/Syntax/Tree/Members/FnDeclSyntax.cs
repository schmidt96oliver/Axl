using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ParamSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.Param, children)
{
    public IdentifierToken Name => NthChildOfType<IdNameSyntax>(0).Token;
    
    public TypeNameSyntax? TypeAnnotation => NthNodeOfKindOrNull(SyntaxKind.TypeAnnotationClause, 0)?
        .NthChildOfType<TypeNameSyntax>(0);
}

public sealed class FnDeclSyntax(ImmutableArray<SyntaxElement> children)
    : MemberDeclSyntax(SyntaxKind.FnDecl, children)
{
    public IEnumerable<Token> Modifiers
        => Children.OfType<Token>().Where(token => token.Kind.IsModifier);

    public StringExprSyntax? NativeName
        => NthNodeOfKindOrNull(SyntaxKind.NativeClause, 0)?
            .NthChildOfType<StringExprSyntax>(0);
    
    public IdentifierToken Name => NthChildOfType<IdNameSyntax>(0).Token;

    public IEnumerable<ParamSyntax> Parameters
        => NthNodeOfKind(SyntaxKind.ParamList, 0)
            .Children.OfType<ParamSyntax>();

    public TypeNameSyntax? ReturnTypeAnnotation
        => NthNodeOfKindOrNull(SyntaxKind.TypeAnnotationClause, 0)?
            .NthChildOfType<TypeNameSyntax>(0);
    
    /// <summary>
    /// Only <c>null</c>, if <see cref="NativeName"/> is not <c>null</c>.
    /// </summary>
    public BodySyntax? Body => NthChildOfTypeOrNull<BodySyntax>(0);
}