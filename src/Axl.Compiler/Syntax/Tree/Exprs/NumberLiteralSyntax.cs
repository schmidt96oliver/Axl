using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class NumberLiteralSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.NumberLiteral, children)
{
    public NumberLiteralToken Token => Children.FirstNonTriviaToken() as NumberLiteralToken
                                       ?? throw new InvalidOperationException();

}