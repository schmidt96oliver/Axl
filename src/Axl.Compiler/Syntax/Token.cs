using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Syntax;

public class Token : SyntaxElement
{
    public TokenKind Kind { get; }
    public override SourceSpan Span { get; }
    public override SourceSpan? SyntaxSpan => Kind.IsTrivia ? null : Span;

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
    /// Creates a token that carries no value.
    /// </summary>
    /// <exception cref="ArgumentException">If <paramref name="kind"/> carries a value. It must be constructed through special constructors.</exception>
    public static Token Simple(SourceSpan span, TokenKind kind)
    {
        Guard.MustBe(kind is not
                (TokenKind.Identifier or TokenKind.NumberLiteral or TokenKind.StringText or TokenKind.Error),
            "Construct through specialized static methods.");
        
        return new Token(span, kind);
    }

    public static IdentifierToken Identifier(SourceSpan span, Identifier id)
        => new IdentifierToken(span, id);

    public static NumberLiteralToken NumberLiteral(SourceSpan span, string body, NumberLiteralSuffix suffix)
        => new NumberLiteralToken(span, body, suffix);

    public static StringTextToken StringText(SourceSpan span, string processedText)
        => new StringTextToken(span, processedText);

    public static Token Error(ErrorGuaranteed proof, SourceSpan span)
        => new Token(span, TokenKind.Error);
}