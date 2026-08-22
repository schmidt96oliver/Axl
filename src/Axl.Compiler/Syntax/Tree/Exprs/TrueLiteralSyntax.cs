using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class TrueLiteralSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.TrueLiteral, children);