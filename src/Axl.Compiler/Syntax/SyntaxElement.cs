namespace Axl.Compiler.Syntax;

public abstract class SyntaxElement
{
    /// <summary>
    /// The full span inside source text, including trivia.
    /// </summary>
    public abstract SourceSpan Span { get; }
    
    /// <summary>
    /// Span inside source text, excluding leading and trailing trivia.
    /// <c>null</c> if this element only consists of trivia.
    /// </summary>
    public abstract SourceSpan? SyntaxSpan { get; }
}