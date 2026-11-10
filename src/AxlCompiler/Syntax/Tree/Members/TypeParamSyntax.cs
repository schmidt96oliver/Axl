using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class TypeParamSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.TypeParam, children)
{
    public IdentifierToken Name => Children.FirstOfType<NameRefSyntax>().Token;
}