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
    
    /// <summary>
    /// Only <c>null</c>, if <see cref="NativeName"/> is not <c>null</c>.
    /// </summary>
    public BodySyntax Body => Children.FirstOfType<BodySyntax>();
}