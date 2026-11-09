using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class StructBodySyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.StructBody, children)
{
}