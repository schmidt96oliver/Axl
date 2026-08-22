using System.Collections.Immutable;
using Dunet;

namespace Axl.Compiler.Syntax.Tree;

[Union]
public partial record StringPart
{
    public partial record Text(StringTextToken Token);

    public partial record Interpolation(ExprSyntax Expr);

    public static StringPart From(SyntaxNode node)
        => node.Kind switch
        {
            SyntaxKind.StringInterpolation => new Interpolation(node.NthChildOfType<ExprSyntax>(0)),
            SyntaxKind.StringText => new Text(node.NthToken(0) as StringTextToken
                                              ?? throw new ArgumentException(
                                                  $"Token on {nameof(SyntaxKind.StringText)} was not {nameof(StringTextToken)}",
                                                  nameof(node))),

            _ => throw new ArgumentException($"{nameof(node)} is not a string part.", nameof(node))
        };
}

public sealed class StringExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.StringExpr, children)
{
    public IEnumerable<StringPart> Parts
        => Children
            .OfType<SyntaxNode>()
            .Where(node => node.Kind is SyntaxKind.StringText or SyntaxKind.StringInterpolation)
            .Select(StringPart.From);
}