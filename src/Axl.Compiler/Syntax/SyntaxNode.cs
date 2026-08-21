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
    
    
    /// <summary>
    /// Enumerates children that have a syntax role. It excludes trivia
    /// token and garbage nodes. Since the parser inserts missing items,
    /// slots in this enumeration are stable.
    /// </summary>
    protected IEnumerable<SyntaxElement> SyntaxChildren()
        => Children
            .Where(child => child is Token { Kind.IsTrivia: false } 
                or SyntaxNode { Kind: not SyntaxKind.Garbage });
    
    /// <summary>
    /// Returns the <paramref name="n"/>th child. Throws, if it is null
    /// or not of type <typeparamref name="T"/>.
    /// </summary>
    protected T NthSlot<T>(int n)
        where T : SyntaxElement
        => NthSlotOrNull<T>(n) ?? throw new ArgumentException($"Slot {n} is not present.", nameof(n));

    /// <summary>
    /// Returns the <paramref name="n"/>th syntax child or <c>null</c> if it is not
    /// present. Throws if present, but not type <typeparamref name="T"/>.
    /// </summary>
    protected T? NthSlotOrNull<T>(int n)
        where T : SyntaxElement
    {
        var child = SyntaxChildren()
            .Skip(n)
            .FirstOrDefault();
        if (child is null)
            return null;
        
        Guard.MustBe(child is T, $"Slot {n} is {child.GetType().Name}, but expected {typeof(T).Name}");
        return (T)child;
    }
}