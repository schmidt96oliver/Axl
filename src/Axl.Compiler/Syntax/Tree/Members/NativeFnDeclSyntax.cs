using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public class NativeFnDeclSyntax(ImmutableArray<SyntaxElement> children)
    : MemberSyntax(SyntaxKind.NativeFnDecl, children)
{
    public StringExprSyntax NativeName
        => Children.FirstOfKind(SyntaxKind.NativeClause)
            .Children.FirstOfType<StringExprSyntax>();
    
    public IdentifierToken Name => Children.FirstOfType<IdNameSyntax>().Token;
    
    public IEnumerable<ParamSyntax> Parameters
        => Children.FirstOfKind(SyntaxKind.ParamList)
            .Children.OfType<ParamSyntax>();

    public TypeNameSyntax? ReturnTypeAnnotation
        => Children.FirstOfKindOrNull(SyntaxKind.TypeAnnotationClause)?
            .Children.FirstOfType<TypeNameSyntax>();
}