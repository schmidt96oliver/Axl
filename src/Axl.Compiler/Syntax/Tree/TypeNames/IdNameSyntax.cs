using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class IdNameSyntax(ImmutableArray<SyntaxElement> children)
    : TypeNameSyntax(SyntaxKind.IdName, children)
{
    public IdentifierToken Token => NthToken(0) as IdentifierToken
                                    ?? throw new ArgumentException(
                                        $"Token on {nameof(IdNameSyntax)} was not {nameof(IdentifierToken)}",
                                        nameof(children));
}