using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class BlockExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.BlockExpr, children);