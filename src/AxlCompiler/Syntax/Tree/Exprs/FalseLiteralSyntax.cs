using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class FalseLiteralSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.FalseLiteral, children);