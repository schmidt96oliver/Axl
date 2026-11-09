using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public closed class StmtSyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : SyntaxNode(kind, children);