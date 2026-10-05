using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class BlockExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.BlockExpr, children);