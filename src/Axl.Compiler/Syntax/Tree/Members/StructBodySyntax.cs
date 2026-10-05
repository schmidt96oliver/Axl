using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class StructBodySyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.StructBody, children)
{
}