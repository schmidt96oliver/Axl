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
            var (endLine, endCol) = location.File.GetLinePositionOrEof(location.Span.End);
            return new Range(startLine, startCol, endLine, endCol);
        }
    }
}