using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class NumberLiteralSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.NumberLiteral, children)
{
    public Token Token => NthToken(0);
}