using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class TypeParamSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.TypeParam, children)
{
    public IdentifierToken Name => Children.FirstOfType<IdNameSyntax>().Token;
}