using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class NamespaceNameSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.NamespaceName, children)
{
    public IEnumerable<Token> Identifiers => Children
        .Where(c => c is Token { Kind: TokenKind.Identifier })
        .Cast<Token>();
}