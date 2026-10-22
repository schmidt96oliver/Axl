using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class NamespaceNameSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.NamespaceName, children)
{
    public IEnumerable<IdNameSyntax> Parts => Children.OfType<IdNameSyntax>();
}