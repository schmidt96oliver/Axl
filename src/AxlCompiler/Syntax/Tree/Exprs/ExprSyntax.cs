using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public closed class ExprSyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : SyntaxNode(kind, children);