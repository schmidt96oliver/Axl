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
            var isEndAtEof = location.End >= location.SourceText.Length;
            
            var firstColumn = location.StartColumn;
            var endLine = location.SourceText.Lines[location.EndLine];
            var endColumn = location.EndColumn;
            
            // --- Special-case empty spans
            // To prevent the editor snapping back to the previous word, we need to
            // extend the range by one character. If that is out of the line range,
            // snap back one character. If that is also not possible, report the 
            // empty range and leave it to the editor.
            if (location.Range.Length == 0)
            {
                if (isEndAtEof && firstColumn > 0)
                        firstColumn--;
                else if (!isEndAtEof)
                {
                    if (endColumn < endLine.Length)
                        endColumn++;
                    else if (firstColumn > 0)
                        firstColumn--;
                }
            }
            
            return new Range(location.StartLine, firstColumn, location.EndLine, endColumn);
        }
    }
}