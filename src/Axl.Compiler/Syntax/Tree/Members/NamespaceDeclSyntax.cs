using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class NamespaceDeclSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.NamespaceDecl, children)
{
    public NamespaceNameSyntax Name => Children.FirstOfType<NamespaceNameSyntax>();
}