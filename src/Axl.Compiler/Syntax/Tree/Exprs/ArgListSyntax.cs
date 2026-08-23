using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ArgListSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.ArgList, children)
{
    public IEnumerable<ArgSyntax> Arguments 
        => Children.OfType<ArgSyntax>();
}