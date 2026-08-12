using Axl.Compiler;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Axl.Lsp;

public static class SourceLocationConversion
{
    extension(SourceLocation location)
    {
        public Range ToLsp()
        {
            var (startLine, startCol) = location.File.GetLinePositionOrEof(location.Span.First);

            var isEndAtEof = location.Span.End >= location.File.Text.Length;
            
            LineInfo? endLineInfo = !isEndAtEof ? location.File.GetLineAt(location.Span.End) : null;
            var (endLine, endCol) = endLineInfo is LineInfo info
                ? (info.LineNumber, location.Span.End - info.Span.First)
                : (location.File.EofLinePosition.Line, location.File.EofLinePosition.Column);

            // --- Special-case empty spans
            // To prevent the editor snapping back to the previous word, we need to
            // extend the range by one character. If that is out of the line range,
            // snap back one character. If that is also not possible, report the 
            // empty span and leave it to the editor.
            if (location.Span.Length == 0)
            {
                if (isEndAtEof && startCol > 0)
                        startCol--;
                else if (!isEndAtEof)
                {
                    if (endCol < endLineInfo!.Value.LengthWithoutEnding)
                        endCol++;
                    else if (startCol > 0)
                        startCol--;
                }
            }
            
            return new Range(startLine, startCol, endLine, endCol);
        }
    }
}