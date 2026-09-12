using System.Diagnostics;
using Axl.Compiler.Text;

namespace Axl.Compiler.Syntax;

public class Token : SyntaxElement
{
    public TokenKind Kind { get; }
    public sealed override SourceRange FullRange { get; }
    public sealed override SourceRange? Range => Kind.IsTrivia ? null : FullRange;
    public bool IsMissing { get; }

    /// <summary>
    /// Some tokens carry a value and must be constructed as a derived type
    /// like <see cref="IdentifierToken"/>, <see cref="NumberLiteralToken"/>, <see cref="StringTextToken"/>.
    /// Thus, construction must go through dedicated static methods below, so that
    /// <see cref="TokenKind.Identifier"/> always is a <see cref="IdentifierToken"/> and so on.
    /// </summary>
    protected Token(SourceRange range, TokenKind kind, bool isMissing = false)
    {
        if (isMissing) Guard.MustBe(range.IsEmpty);

        Kind = kind;
        FullRange = range;
        IsMissing = isMissing;
    }


    /// <summary>
    /// Returns this <see cref="Token"/> with a different <paramref name="kind"/>.
    /// <paramref name="kind"/> must not carry a value.
    /// </summary>
    public Token WithKind(TokenKind kind)
    {
        Guard.MustBe(!kind.HasValue);
        return new Token(FullRange, kind);
    }
    
    
    /// <summary>
    /// Creates a token that carries no value.
    /// </summary>
    /// <exception cref="ArgumentException">If <paramref name="kind"/> carries a value. It must be constructed through special constructors.</exception>
    public static Token MakeSimple(SourceRange range, TokenKind kind)
    {
        Guard.MustBe(!kind.HasValue,
            "Construct through specialized static methods.");
        
        return new Token(range, kind);
    }

    /// <summary>
    /// Creates a missing token of the specified <paramref name="kind"/>.
    /// <paramref name="kind"/> can carry a value. In this case, a token of
    /// the specific type with empty value is returned.
    /// </summary>
    public static Token MakeMissing(SourceRange range, TokenKind kind)
    {
        Guard.MustBe(range.IsEmpty);
        
        switch (kind)
        {
            case TokenKind.Identifier:
                return new IdentifierToken(range, string.Empty);
            case TokenKind.NumberLiteral:
                return new NumberLiteralToken(range, body: string.Empty, NumberLiteralSuffix.None);
            case TokenKind.StringText:
                return new StringTextToken(range, processedText: string.Empty, isMissing: true);
            
            default:
                return new Token(range, kind, isMissing: true);
        }
    }
    
    public static IdentifierToken MakeIdentifier(SourceRange range, string identifier)
        => new(range, identifier);

    public static NumberLiteralToken MakeNumberLiteral(SourceRange range, string body, NumberLiteralSuffix suffix)
        => new(range, body, suffix);

    public static StringTextToken MakeStringText(SourceRange range, string processedText)
        => new(range, processedText);
}