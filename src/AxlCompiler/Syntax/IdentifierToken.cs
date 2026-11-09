using AxlCompiler.Text;

namespace AxlCompiler.Syntax;

public sealed class IdentifierToken(SourceRange range, string identifier)
    : Token(range, TokenKind.Identifier, isMissing: identifier.Length == 0)
{
    public string Identifier { get; } = identifier;
}