using Axl.Compiler;
using Axl.Compiler.Text;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Axl.Lsp;

public static class SourceLocationConversion
{
    extension(SourceLocation location)
    {
        public Range ToLsp()
        {
            var (startLine, startCol) = location.SourceText.GetLinePositionOrEof(location.Range.First);

            var isEndAtEof = location.Range.End >= location.SourceText.Text.Length;
            
            LineInfo? endLineInfo = !isEndAtEof ? location.SourceText.GetLineAt(location.Range.End) : null;
            var (endLine, endCol) = endLineInfo is LineInfo info
                ? (info.LineNumber, location.Range.End - info.Range.First)
                : (location.SourceText.EofLinePosition.Line, location.SourceText.EofLinePosition.Column);

            // --- Special-case empty spans
            // To prevent the editor snapping back to the previous word, we need to
            // extend the range by one character. If that is out of the line range,
            // snap back one character. If that is also not possible, report the 
            // empty range and leave it to the editor.
            if (location.Range.Length == 0)
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