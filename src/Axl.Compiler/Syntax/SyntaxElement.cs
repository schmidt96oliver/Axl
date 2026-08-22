namespace Axl.Compiler.Syntax;

public abstract class SyntaxElement
{
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
}