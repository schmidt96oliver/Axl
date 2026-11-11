using System.Diagnostics;
using AxlCompiler.Text;

namespace AxlCompiler.Syntax;

public closed class SyntaxElement
{
    private bool _wasParentSet = false;
    
    /// <summary>
    /// Set during construction of <see cref="SyntaxNode"/>. Will assert on
    /// access, if accessed on a token that is not part of a syntax tree.
    /// </summary>
    public SyntaxNode? Parent
    {
        get
        {
            Debug.Assert(_wasParentSet, "Parent was not set during construction.");
            return field;
        }
        internal set
        {
            Debug.Assert(!_wasParentSet, "Parent has already been set.");
            _wasParentSet = true;
            field = value;
        }
    }

    public virtual SyntaxTree Tree
    {
        get
        {
            Debug.Assert(Parent is not null, "Tree must be overriden on root node.");
            return Parent.Tree;
        }
        // ReSharper disable once ValueParameterNotUsed
        internal set => Debug.Fail("Must be overriden.");
    }
    
    
    /// <summary>
    /// Including trivia.
    /// Full ranges tile the source without gaps, which is what makes the
    /// tree lossless. Use it to reproduce source text.
    /// </summary>
    public abstract SourceRange FullRange { get; }
    
    /// <summary>
    /// Excluding leading and trailing trivia.
    /// <c>null</c> if this element consists only of trivia.
    /// </summary>
    public abstract SourceRange? Range { get; }


    public SourceLocation Location => new(Tree.SourceText, Range ?? FullRange);

    public ReadOnlySpan<char> Text => Location.Text;


    public IEnumerable<SyntaxNode> Ancestors()
    {
        for (var ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent)
            yield return ancestor;
    }

    /// <summary>
    /// Traverse this node in pre-order depth-first order,
    /// so elements appear in source order. Includes this node,
    /// if not excluded via <paramref name="excludeSelf"/>.
    /// </summary>
    public IEnumerable<SyntaxElement> Traverse(bool excludeSelf = false)
    {
        var stack = new Stack<SyntaxElement>();
        stack.Push(this);

        while (stack.TryPop(out var current))
        {
            if (!excludeSelf || current != this)
                yield return current;
            if (current is SyntaxNode currentNode)
            {
                for (var i = currentNode.Children.Length - 1; i >= 0; i--)
                    stack.Push(currentNode.Children[i]);
            }
        }
    }

    /// <summary>
    /// Traverse all tokens of this node in pre-order depth-first order,
    /// so tokens appear in source order. Includes this token,
    /// if not excluded via <paramref name="excludeSelf"/>.
    /// </summary>
    public IEnumerable<Token> TraverseTokens(bool excludeSelf = false)
        => Traverse(excludeSelf).OfType<Token>();
}