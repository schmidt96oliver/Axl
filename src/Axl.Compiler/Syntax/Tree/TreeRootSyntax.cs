using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class TreeRootSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.TreeRoot, children)
{
    public IEnumerable<StmtOrMemberSyntax> Items
        => Children.OfType<StmtOrMemberSyntax>();
}