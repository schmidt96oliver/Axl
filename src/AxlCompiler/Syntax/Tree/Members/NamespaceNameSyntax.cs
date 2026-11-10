using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class NamespaceNameSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.NamespaceName, children)
{
    public IEnumerable<NameRefSyntax> Parts => Children.OfType<NameRefSyntax>();
}