using System.Collections.Immutable;
using System.Diagnostics;

namespace Axl.Compiler.Syntax.Tree;

public sealed class FileSyntax(ImmutableArray<SyntaxElement> children)
    : SyntaxNode(SyntaxKind.File, children)
{
    private SyntaxTree? _tree;

    public override SyntaxTree Tree
    {
        get
        {
            Debug.Assert(_tree is not null, "Tree has not been set during construction.");
            return _tree;
        }
        internal set
        {
            Debug.Assert(_tree is null, "Tree has already been set.");
            _tree = value;
        }
    }
}