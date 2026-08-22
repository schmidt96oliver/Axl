using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class AssignExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.AssignExpr, children)
{
    public ExprSyntax Target => Children.FirstOfType<ExprSyntax>();
    public ExprSyntax Value => Children.SecondOfType<ExprSyntax>();
}