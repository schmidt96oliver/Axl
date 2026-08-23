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