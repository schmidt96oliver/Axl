using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class TrueLiteralSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.TrueLiteral, children);