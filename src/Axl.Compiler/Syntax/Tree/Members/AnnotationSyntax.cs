using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class AnnotationSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.Annotation, children)
{
    public IdNameSyntax Name => Children.FirstOfType<IdNameSyntax>();
}