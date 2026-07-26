namespace Axl.Compiler.Syntax;

public sealed class IdentifierToken(SourceSpan span, Identifier id)
    : Token(span, TokenKind.Identifier)
{
    public Identifier Id { get; } = id;
}