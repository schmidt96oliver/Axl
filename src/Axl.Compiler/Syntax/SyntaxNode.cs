using System.Collections.Immutable;

namespace Axl.Compiler.Syntax;

public class SyntaxNode : SyntaxElement
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

    public IEnumerable<SyntaxNode> NodesOfKind(SyntaxKind kind)
        => Children.OfType<SyntaxNode>().Where(node => node.Kind == kind);
    
    public SyntaxNode? NthNodeOfKindOrNull(SyntaxKind kind, int n)
        => NodesOfKind(kind).Skip(n).FirstOrDefault();

    public SyntaxNode NthNodeOfKind(SyntaxKind kind, int n)
        => NthNodeOfKindOrNull(kind, n)
           ?? throw new ArgumentException($"Node does not have {n} nodes of kind {kind}.", nameof(n));
    
    public T? NthChildOfTypeOrNull<T>(int n)
        where T : SyntaxElement
        => Children.OfType<T>()
            .Skip(n)
            .FirstOrDefault();

    public T NthChildOfType<T>(int n)
        where T: SyntaxElement
        => NthChildOfTypeOrNull<T>(n) ??
           throw new ArgumentException($"Node does not have {n} nodes of type {typeof(T).Name}.", nameof(n));
    
    public Token? NthTokenOrNull(int n)
        => Children.OfType<Token>()
            .Where(t => !t.Kind.IsTrivia)
            .Skip(n)
            .FirstOrDefault();
    
    public Token NthToken(int n)
        => NthTokenOrNull(n) ?? 
           throw new ArgumentException($"Node does not have {n} tokens.", nameof(n));
    
    public IEnumerable<SyntaxElement> ChildrenAfter(TokenKind token)
        => Children
            .SkipWhile(child => !(child is Token childToken && childToken.Kind == token))
            .Skip(1); // Skip the token itself
}