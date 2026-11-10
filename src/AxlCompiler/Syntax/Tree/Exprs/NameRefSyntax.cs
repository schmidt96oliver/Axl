using System.Collections.Immutable;

namespace AxlCompiler.Syntax.Tree;

public sealed class NameRefSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.NameRef, children)
{
    public IdentifierToken Token => Children.FirstNonTriviaToken() as IdentifierToken
                                    ?? throw new ArgumentException(
                                        $"Token on {nameof(NameRefSyntax)} was not {nameof(IdentifierToken)}",
                                        nameof(children));
}