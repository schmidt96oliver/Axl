using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;


public sealed class TypeNameSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.TypeName, children)
{
    public IEnumerable<IdNameSyntax> Parts
        => Children.OfType<IdNameSyntax>();
}