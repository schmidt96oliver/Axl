using System.Collections.Immutable;
using Axl.Compiler.Text;

namespace Axl.Compiler.Syntax;

public abstract class SyntaxNode : SyntaxElement
{
    public SyntaxKind Kind { get; }
    
    public ImmutableArray<SyntaxElement> Children { get; }
    
    /// <inheritdoc/>
    public override SourceRange FullRange { get; }
    
    /// <inheritdoc/>
    public override SourceRange? Range { get; }


    /// <param name="children">
    /// Must be non-empty. Every node covers at least one token, so there
    /// are no empty nodes.
    /// </param>
    internal SyntaxNode(SyntaxKind kind, ImmutableArray<SyntaxElement> children)
    {
        Guard.MustBe(!children.IsDefaultOrEmpty, "A node must have children.");

        Kind = kind;
        Children = children;
        
        FullRange = SourceRange.FromTo(children[0].FullRange, children[^1].FullRange);
        
        // Set parents
        foreach (var child in children)
            child.Parent = this;
        
        // Calculate Range
        if (children.FirstOrDefault(element => element.Range is not null) is SyntaxElement firstNonTrivia)
        {
            // Since there was a first element, Last will always find something.
            var lastNonTrivia = children.Last(element => element.Range is not null);
            Range = SourceRange.FromTo(firstNonTrivia.Range!.Value,
                lastNonTrivia.Range!.Value);
        }
        else
            Range = null;
    }


    /// <summary>
    /// Enumerates all elements relevant for syntax. That excludes trivia,
    /// garbage nodes and unknown character tokens.
    /// </summary>
    public IEnumerable<SyntaxElement> SyntaxElements()
        => Children.Where(element =>
            element is not (Token { Kind.IsTrivia: true } or Token { Kind: TokenKind.UnknownCharacters }
                or SyntaxNode { Kind: SyntaxKind.Garbage }));
    
    /// <summary>
    /// Enumerates all nodes relevant for syntax. That excludes gargabe nodes.
    /// </summary>
    public IEnumerable<SyntaxNode> SyntaxNodes()
        => Children.OfType<SyntaxNode>().Where(node => node.Kind is not SyntaxKind.Garbage);
}