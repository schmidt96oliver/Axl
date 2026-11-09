using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class GarbageSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.Garbage, children);