using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class QualifiedNameSyntax(ImmutableArray<SyntaxElement> children)
    : TypeNameSyntax(SyntaxKind.QualifiedName, children)
{
    public IEnumerable<IdentifierToken> Parts
        => Children.OfType<IdNameSyntax>().Select(idNameSyntax => idNameSyntax.Token);
}