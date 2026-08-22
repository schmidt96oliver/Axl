using Axl.Compiler;
using Axl.Compiler.Syntax;

namespace Axl.Tests;

/// <summary>
/// Tree walks shared by the tests. The compiler does not expose enumerators yet.
/// </summary>
public static class SyntaxWalk
{
    /// <summary>
    /// <paramref name="node"/> and all its descendants, parents before children.
    /// </summary>
    public static IEnumerable<SyntaxNode> AllNodesRecursive(SyntaxNode node)
    {
        yield return node;

        foreach (var child in node.Children.OfType<SyntaxNode>())
        foreach (var childNode in AllNodesRecursive(child))
            yield return childNode;
    }

    /// <summary>
    /// Spans of all tokens under <paramref name="element"/>, in document order.
    /// Includes trivia and missing (empty) tokens.
    /// </summary>
    public static IEnumerable<SourceSpan> AllTokenSpansRecursive(SyntaxElement element)
    {
        if (element is Token token)
            yield return token.FullSpan;
        else if (element is SyntaxNode node)
        {
            var childTokenSpans = node.Children.SelectMany(AllTokenSpansRecursive);
            foreach (var childTokenSpan in childTokenSpans)
                yield return childTokenSpan;
        }
    }
}
