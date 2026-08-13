namespace Axl.Compiler.Syntax;

public sealed class StringTextToken(SourceSpan span, string processedText, bool isMissing = false) 
    : Token(span, TokenKind.StringText, isMissing)
{
    /// <summary>
    /// Escapes have been removed.
    /// </summary>
    public string ProcessedText { get; } = processedText;
}