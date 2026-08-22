using System.Collections.Immutable;

namespace Axl.Compiler.Syntax.Tree;

public abstract class StringPartSyntax(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    : SyntaxNode(kind, children);

public sealed class StringTextSyntax(ImmutableArray<SyntaxElement> children)
    : StringPartSyntax(SyntaxKind.StringText, children)
{
    public StringTextToken Text => NthToken(0) as StringTextToken
                                   ?? throw new ArgumentException(
                                       $"Token on {nameof(StringTextSyntax)} was not {nameof(StringTextToken)}",
                                       nameof(children));
}

public sealed class StringInterpolationSyntax(ImmutableArray<SyntaxElement> children)
    : StringPartSyntax(SyntaxKind.StringInterpolation, children)
{
    /// <summary>
    /// <c>null</c>, if the interpolation is empty.
    /// </summary>
    public ExprSyntax? Expr => NthChildOfTypeOrNull<ExprSyntax>(0);
}

public sealed class StringExprSyntax(ImmutableArray<SyntaxElement> children)
    : ExprSyntax(SyntaxKind.StringExpr, children)
{
    public IEnumerable<StringPartSyntax> Parts
        => Children
            .OfType<StringPartSyntax>();
}