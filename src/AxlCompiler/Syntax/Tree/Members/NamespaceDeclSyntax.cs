using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class NamespaceDeclSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.NamespaceDecl, children)
{
    public NamespaceNameSyntax Name => Children.FirstOfType<NamespaceNameSyntax>();
}