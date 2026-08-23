using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public abstract class MemberSyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : SyntaxNode(kind, children)
{
    public IEnumerable<Token> Modifiers
        => Children.OfType<Token>().Where(token => token.Kind.IsModifier);
}