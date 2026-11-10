using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class AnnotationSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.Annotation, children)
{
    public NameRefSyntax NameRef => Children.FirstOfType<NameRefSyntax>();
}