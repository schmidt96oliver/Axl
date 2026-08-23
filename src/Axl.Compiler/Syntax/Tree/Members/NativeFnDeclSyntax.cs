using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class NativeFnDeclSyntax(ImmutableArray<SyntaxElement> children)
    : MemberSyntax(SyntaxKind.NativeFnDecl, children)
{
    public StringExprSyntax NativeName
        => Children.FirstOfType<NativeClauseSyntax>().NativeName;
    
    public IdentifierToken Name => Children.FirstOfType<IdNameSyntax>().Token;
    
    public IEnumerable<ParamSyntax> Parameters
        => Children.FirstOfType<ParamListSyntax>().Parameters;

    public TypeNameSyntax? ReturnTypeAnnotation
        => Children.FirstOfTypeOrNull<TypeAnnotationClauseSyntax>()?.TypeName;
}