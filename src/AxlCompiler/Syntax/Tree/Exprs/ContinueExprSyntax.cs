using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class ContinueExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.ContinueExpr, children);