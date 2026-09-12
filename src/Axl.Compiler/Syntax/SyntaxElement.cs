using System.Diagnostics;
using Axl.Compiler.Text;

namespace Axl.Compiler.Syntax;

public abstract class SyntaxElement
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


    public SourceLocation Location => SourceLocation.From(Tree.SourceText, Range ?? FullRange);

    public ReadOnlySpan<char> Text => Location.Text;
}