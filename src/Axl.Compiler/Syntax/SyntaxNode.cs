using System.Collections.Immutable;
using System.Diagnostics;

namespace Axl.Compiler.Syntax;

public sealed class SyntaxNode : SyntaxElement
{
    public SyntaxKind Kind { get; }
    
    public ImmutableArray<SyntaxElement> Children { get; }
    
    /// <inheritdoc/>
    public override SourceSpan Span { get; }
    
    /// <inheritdoc/>
    public override SourceSpan? SyntaxSpan { get; }


    /// <summary>
    /// Creates a non-empty node.
    /// </summary>
    /// <param name="children">Must be non-empty</param>
    internal SyntaxNode(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    {
        Guard.MustBe(!children.IsDefaultOrEmpty);

        Kind = kind;
        Children = children;
        
        Span = SourceSpan.FromTo(children[0].Span, children[^1].Span);
        Debug.Assert(Span.IsPartitionedBy(children.Select(c => c.Span)));
        
        // Calculate SyntaxSpan
        if (children.FirstOrDefault(element => element.SyntaxSpan is not null) is SyntaxElement firstNonTrivia)
        {
            // Since there was a first element, Last will always find something.
            var lastNonTrivia = children.Last(element => element.SyntaxSpan is not null);
            SyntaxSpan = SourceSpan.FromTo(firstNonTrivia.SyntaxSpan!.Value,
                lastNonTrivia.SyntaxSpan!.Value);
        }
        else
            SyntaxSpan = null;
    }

    /// <summary>
    /// Creates an empty node at a specific position.
    /// </summary>
    /// <param name="emptySpan">Must have length 0.</param>
    internal SyntaxNode(SyntaxKind kind, SourceSpan emptySpan)
    {
        Guard.MustBe(emptySpan.IsEmpty);

        Children = [];
        Kind = kind;
        Span = emptySpan;
        SyntaxSpan = null;
    }
}