namespace Axl.Compiler.Syntax;

public sealed class IdentifierToken(SourceSpan span, string identifier)
    : Token(span, TokenKind.Identifier, isMissing: identifier.Length == 0)
{
    public string Identifier { get; } = identifier;
}