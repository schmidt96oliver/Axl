using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ParamListSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.ParamList, children)
{
    public IEnumerable<ParamSyntax> Parameters 
        => Children.OfType<ParamSyntax>();
}