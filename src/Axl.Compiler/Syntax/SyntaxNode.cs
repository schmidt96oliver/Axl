using System.Collections.Immutable;

namespace Axl.Compiler.Syntax;

public abstract class SyntaxNode : SyntaxElement
{
    public SyntaxKind Kind { get; }
    
    public ImmutableArray<SyntaxElement> Children { get; }
    
    /// <inheritdoc/>
    public override SourceSpan FullSpan { get; }
    
    /// <inheritdoc/>
    public override SourceSpan? Span { get; }


    /// <param name="children">
    /// Must be non-empty. Every node covers at least one token, so there
    /// are no empty nodes.
    /// </param>
    internal SyntaxNode(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    {
        Guard.MustBe(!children.IsDefaultOrEmpty, "A node must have children.");

        Kind = kind;
        Children = children;
        
        FullSpan = SourceSpan.FromTo(children[0].FullSpan, children[^1].FullSpan);
        
        // Set parents
        foreach (var child in children)
            child.Parent = this;
        
        // Calculate Span
        if (children.FirstOrDefault(element => element.Span is not null) is SyntaxElement firstNonTrivia)
        {
            // Since there was a first element, Last will always find something.
            var lastNonTrivia = children.Last(element => element.Span is not null);
            Span = SourceSpan.FromTo(firstNonTrivia.Span!.Value,
                lastNonTrivia.Span!.Value);
        }
        else
            Span = null;
    }
}