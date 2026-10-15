using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class FunNameSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.FunName, children)
{
    public Token Token => Children.FirstNonTriviaToken();
}