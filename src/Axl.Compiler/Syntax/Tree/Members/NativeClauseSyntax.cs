using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class NativeClauseSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.NativeClause, children)
{
    public StringExprSyntax NativeName => Children.FirstOfType<StringExprSyntax>();
}