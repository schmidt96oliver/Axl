using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ContinueExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.ContinueExpr, children);