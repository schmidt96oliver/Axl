using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ArgSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.Arg, children)
{
    public ExprSyntax Expr => Children.FirstOfType<ExprSyntax>();
}