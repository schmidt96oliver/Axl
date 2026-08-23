using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class GarbageSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.Garbage, children);