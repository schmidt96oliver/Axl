using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class TypeParamListSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.TypeParamList, children)
{
    public IEnumerable<TypeParamSyntax> TypeParameters 
        => Children.OfType<TypeParamSyntax>();
}