using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class PathSyntax(ImmutableArray<SyntaxElement> children)
    : TypeNameSyntax(SyntaxKind.Path, children)
{
    public IEnumerable<IdentifierToken> Parts
        => Children.OfType<IdNameSyntax>().Select(idNameSyntax => idNameSyntax.Token);
}