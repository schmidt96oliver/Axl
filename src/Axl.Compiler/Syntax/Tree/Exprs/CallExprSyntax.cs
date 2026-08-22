using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public sealed class CallExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.CallExpr, children)
{
    public ExprSyntax Callee => Children.FirstOfType<ExprSyntax>();

    public IEnumerable<ExprSyntax> Arguments => 
        Children.FirstOfKind(SyntaxKind.ArgList)
        .Children.OfKind(SyntaxKind.Arg)
        .Select(node => node.Children.FirstOfType<ExprSyntax>());
}