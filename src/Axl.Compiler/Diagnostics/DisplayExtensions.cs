using System.Diagnostics;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Diagnostics;

public static class DisplayExtensions
{
    extension(TokenKind kind)
    {
        public string DisplayName => kind switch
        {
            TokenKind.Identifier => "an identifier",
            TokenKind.Comment => "a comment",
            TokenKind.Whitespace => "whitespace",
            TokenKind.UnknownCharacters => "unknown characters",
            TokenKind.Eof => "end of file",
            TokenKind.NumberLiteral => "a number",
            TokenKind.StringText => "string text",

            _ => $"'{SyntaxFacts.GetText(kind) ?? throw new UnreachableException($"No DisplayName for TokenKind '{kind}'.")}'"
        };
    }
}