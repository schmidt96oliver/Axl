namespace Axl.Compiler.Syntax;

public enum NumberLiteralSuffix
{
    None,
    I32,
    I64,
    F32,
    F64,
}

public sealed class NumberLiteralToken(SourceSpan span, string body, NumberLiteralSuffix suffix) 
    : Token(span, TokenKind.NumberLiteral, isMissing: body.Length == 0)
{
    /// <summary>
    /// Empty, if <see cref="IsMissing"/> is <c>true</c>.
    /// Otherwise, one of these forms:
    /// <list type="bullet">
    /// <item>0x[0-9A-Fa-f]+</item>
    /// <item>0b[01]+</item>
    /// <item>[0-9]*.[0-9]+</item>
    /// <item>[0-9]+</item>
    /// </list>
    /// Underscores have been removed.
    /// </summary>
    public string Body { get; } = body;

    public NumberLiteralSuffix Suffix { get; } = suffix;
    
    public bool HasDecimalPoint => Body.Contains('.');
}