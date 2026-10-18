using System.Collections.Immutable;
using Axl.Compiler.Text;

namespace Axl.Compiler.Syntax.Tree;

public sealed class ArgListSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.ArgList, children)
{
    public IEnumerable<ArgSyntax> Arguments 
        => Children.OfType<ArgSyntax>();

    public SourceLocation InsideLocation
    {
        get
        {
            var argExprs = Arguments.ToList();
            if (argExprs.Count == 0) return Location;
            return SourceLocation.FromTo(argExprs[0].Location, argExprs[^1].Location);
        }
    }
}