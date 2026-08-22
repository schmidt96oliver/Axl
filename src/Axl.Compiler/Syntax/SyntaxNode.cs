using System.Collections.Immutable;

namespace Axl.Compiler.Syntax;

public class SyntaxNode : SyntaxElement
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
    
    
    protected T? NthChildOfTypeOrNull<T>(int n)
        where T : SyntaxElement
        => Children.OfType<T>()
            .Skip(n)
            .FirstOrDefault();

    protected T NthChildOfType<T>(int n)
        where T: SyntaxElement
        => NthChildOfTypeOrNull<T>(n) ??
           throw new ArgumentException($"Node does not have {n} nodes of type {typeof(T).Name}.", nameof(n));
    
    protected Token? NthTokenOrNull(int n)
        => Children.OfType<Token>()
            .Where(t => !t.Kind.IsTrivia)
            .Skip(n)
            .FirstOrDefault();
    
    protected Token NthToken(int n)
        => NthTokenOrNull(n) ?? 
           throw new ArgumentException($"Node does not have {n} tokens.", nameof(n));
}