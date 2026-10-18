using Axl.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Axl.Lsp;

public static class SourceLocationConversion
{
    public static Range GetLspRange(this SourceLocation location)
        => new(location.StartLine, location.StartColumn,
            location.EndLine, location.EndColumn);
    
    public static SourceRange? TryGetAxlRange(this Range lspRange, SourceText sourceText)
    {
        var maybeStart = lspRange.Start.TryGetIndexInSourceText(sourceText);
        var maybeEnd = lspRange.End.TryGetIndexInSourceText(sourceText);

        if (maybeStart is not int start || maybeEnd is not int end)
            return null;
        return SourceRange.FromBounds(start, end);
    }

    public static int? TryGetIndexInSourceText(this Position lspPosition, SourceText sourceText)
    {
        if (lspPosition.Line < 0 || lspPosition.Line >= sourceText.Lines.Length)
            return null;

        var line = sourceText.Lines[lspPosition.Line];
        if (lspPosition.Character < 0 || lspPosition.Character >= line.Length)
            return null;

        return line.Start + lspPosition.Character;
    }
}