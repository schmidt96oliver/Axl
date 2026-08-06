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


    /// <param name="children">
    /// Must be non-empty. Every node covers at least one token, so there
    /// are no empty nodes.
    /// </param>
    internal SyntaxNode(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    {
        Guard.MustBe(!children.IsDefaultOrEmpty, "A node must have children.");

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
}