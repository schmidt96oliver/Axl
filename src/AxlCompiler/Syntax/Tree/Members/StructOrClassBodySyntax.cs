using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class StructOrClassBodySyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.StructOrClassBody, children)
{
}