namespace Axl.Compiler.Syntax;

public sealed class StringTextToken(SourceSpan span, string processedText) 
    : Token(span, TokenKind.StringText)
{
    /// <summary>
    /// Escapes have been removed.
    /// </summary>
    public string ProcessedText { get; } = processedText;
}