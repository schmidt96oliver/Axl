using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class FalseLiteralSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.FalseLiteral, children);