using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class BreakExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.BreakExpr, children);