using System.Collections.Immutable;

namespace Axl.Compiler.Syntax;

public abstract class SyntaxNode : SyntaxElement
{
    /// <summary>
    /// Non-empty.
    /// </summary>
    public ImmutableArray<SyntaxElement> Children { get; }
    
    /// <inheritdoc/>
    public override SourceSpan Span { get; }
    
    /// <inheritdoc/>
    public override SourceSpan? SyntaxSpan { get; }


    protected SyntaxNode(ImmutableArray<SyntaxElement> children)
    {
        Guard.MustBe(!children.IsDefaultOrEmpty);

        Span = SourceSpan.FromTo(children[0].Span, children[^1].Span);
        Children = children;

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