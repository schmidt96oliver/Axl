using Axl.Compiler.Text;

namespace Axl.Compiler.Syntax;

public sealed class StringTextToken(SourceRange range, string processedText, bool isMissing = false) 
    : Token(range, TokenKind.StringText, isMissing)
{
    /// <summary>
    /// Escapes have been removed.
    /// </summary>
    public string ProcessedText { get; } = processedText;
}