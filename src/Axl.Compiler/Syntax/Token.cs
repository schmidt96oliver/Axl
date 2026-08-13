namespace Axl.Compiler.Syntax;

public class Token : SyntaxElement
{
    public TokenKind Kind { get; }
    public sealed override SourceSpan Span { get; }
    public sealed override SourceSpan? SyntaxSpan => Kind.IsTrivia ? null : Span;

    /// <summary>
    /// Some tokens carry a value and must be constructed as a derived type
    /// like <see cref="IdentifierToken"/>, <see cref="NumberLiteralToken"/>, <see cref="StringTextToken"/>.
    /// Thus, construction must go through dedicated static methods below, so that
    /// <see cref="TokenKind.Identifier"/> always is a <see cref="IdentifierToken"/> and so on.
    /// </summary>
    protected Token(SourceSpan span, TokenKind kind)
    {
        Kind = kind;
        Span = span;
    }


    /// <summary>
    /// Returns this <see cref="Token"/> with a different <paramref name="kind"/>.
    /// <paramref name="kind"/> must not carry a value.
    /// </summary>
    public Token WithKind(TokenKind kind)
    {
        Guard.MustBe(!kind.HasValue);
        return new Token(Span, kind);
    }
    
    
    /// <summary>
    /// Creates a token that carries no value.
    /// </summary>
    /// <exception cref="ArgumentException">If <paramref name="kind"/> carries a value. It must be constructed through special constructors.</exception>
    public static Token Simple(SourceSpan span, TokenKind kind)
    {
        Guard.MustBe(!kind.HasValue,
            "Construct through specialized static methods.");
        
        return new Token(span, kind);
    }

    public static IdentifierToken Identifier(SourceSpan span, Identifier id)
        => new(span, id);

    public static NumberLiteralToken NumberLiteral(SourceSpan span, string body, NumberLiteralSuffix suffix)
        => new(span, body, suffix);

    public static StringTextToken StringText(SourceSpan span, string processedText)
        => new(span, processedText);
}