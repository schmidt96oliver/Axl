using Axl.Compiler.Text;

namespace Axl.Compiler.Syntax;

public sealed class IdentifierToken(SourceRange range, string identifier)
    : Token(range, TokenKind.Identifier, isMissing: identifier.Length == 0)
{
    public string Identifier { get; } = identifier;
}