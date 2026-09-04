using System.Diagnostics;

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
        internal set => Debug.Fail("Must be overriden.");
    }
    
    
    /// <summary>
    /// The span this element covers in source text, including trivia.
    /// Full spans tile the source without gaps, which is what makes the
    /// tree lossless. Use it to reproduce source text.
    /// </summary>
    public abstract SourceSpan FullSpan { get; }
    
    /// <summary>
    /// The span this element covers in source text, excluding leading and
    /// trailing trivia. This is the span to show a user, e.g. in diagnostics
    /// or LSP ranges.
    /// <c>null</c> if this element consists only of trivia.
    /// </summary>
    public abstract SourceSpan? Span { get; }


    public SourceLocation GetLocation()
        => Tree.Source.GetLocation(Span ?? FullSpan);
}