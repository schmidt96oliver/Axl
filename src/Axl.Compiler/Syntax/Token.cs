using System.Diagnostics;

namespace Axl.Compiler.Syntax;

public class Token : SyntaxElement
{
    public TokenKind Kind { get; }
    public sealed override SourceSpan Span { get; }
    public sealed override SourceSpan? SyntaxSpan => Kind.IsTrivia ? null : Span;
    public bool IsMissing { get; }

    /// <summary>
    /// Some tokens carry a value and must be constructed as a derived type
    /// like <see cref="IdentifierToken"/>, <see cref="NumberLiteralToken"/>, <see cref="StringTextToken"/>.
    /// Thus, construction must go through dedicated static methods below, so that
    /// <see cref="TokenKind.Identifier"/> always is a <see cref="IdentifierToken"/> and so on.
    /// </summary>
    protected Token(SourceSpan span, TokenKind kind, bool isMissing = false)
    {
        if (isMissing) Guard.MustBe(span.IsEmpty);

        Kind = kind;
        Span = span;
        IsMissing = isMissing;
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
    public static Token MakeSimple(SourceSpan span, TokenKind kind)
    {
        Guard.MustBe(!kind.HasValue,
            "Construct through specialized static methods.");
        
        return new Token(span, kind);
    }

    /// <summary>
    /// Creates a missing token of the specified <paramref name="kind"/>.
    /// <paramref name="kind"/> can carry a value. In this case, a token of
    /// the specific type with empty value is returned.
    /// </summary>
    public static Token MakeMissing(SourceSpan span, TokenKind kind)
    {
        Guard.MustBe(span.IsEmpty);
        
        switch (kind)
        {
            case TokenKind.Identifier:
                return new IdentifierToken(span, string.Empty);
            case TokenKind.NumberLiteral:
                return new NumberLiteralToken(span, body: string.Empty, NumberLiteralSuffix.None);
            case TokenKind.StringText:
                return new StringTextToken(span, processedText: string.Empty, isMissing: true);
            
            default:
                return new Token(span, kind, isMissing: true);
        }
    }
    
    public static IdentifierToken MakeIdentifier(SourceSpan span, string identifier)
        => new(span, identifier);

    public static NumberLiteralToken MakeNumberLiteral(SourceSpan span, string body, NumberLiteralSuffix suffix)
        => new(span, body, suffix);

    public static StringTextToken MakeStringText(SourceSpan span, string processedText)
        => new(span, processedText);
}