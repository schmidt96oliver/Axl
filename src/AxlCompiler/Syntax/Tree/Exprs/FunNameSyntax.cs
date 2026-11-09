using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class FunNameSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.FunName, children)
{
    public Token Token => Children.FirstNonTriviaToken();
}