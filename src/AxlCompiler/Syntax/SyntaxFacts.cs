using System.Diagnostics;

namespace AxlCompiler.Syntax;

public static class SyntaxFacts
{
    public static string? GetText(TokenKind kind) => kind switch
    {
        // --- Keywords
        TokenKind.FunKw => "fun",
        TokenKind.VarKw => "var",
        TokenKind.NamespaceKw => "namespace",
        TokenKind.ReturnKw => "return",
        TokenKind.IfKw => "if",
        TokenKind.ElseKw => "else",
        TokenKind.WhileKw => "while",
        TokenKind.BreakKw => "break",
        TokenKind.ContinueKw => "continue",
        TokenKind.UsingKw => "using",
        TokenKind.LetKw => "let",
        TokenKind.StructKw => "struct",
        TokenKind.PubKw => "pub",
        TokenKind.StaticKw => "static",
        TokenKind.SelfKw => "self",
        TokenKind.MutKw => "mut",
    
        // --- Literals
        TokenKind.StringStart => "\"",
        TokenKind.StringEnd => "\"",
        TokenKind.TrueKw => "true",
        TokenKind.FalseKw => "false",
    
        // --- Symbols
        TokenKind.Dot => ".",
        TokenKind.Comma => ",",
        TokenKind.Semicolon => ";",
        TokenKind.Colon => ":",
        TokenKind.DoubleAmpersand => "&&",
        TokenKind.DoubleVerticalBar => "||",
        TokenKind.Bang => "!",
        TokenKind.At => "@",
    
        // --- Assignment Symbols
        TokenKind.Equal => "=",
    
        // --- Bracket Symbols
        TokenKind.OpenParen => "(",
        TokenKind.CloseParen => ")",
        TokenKind.OpenBrace => "{",
        TokenKind.CloseBrace => "}",
        TokenKind.OpenBracket => "[",
        TokenKind.CloseBracket => "]",
    
        // --- Mathematical Symbols
        TokenKind.Plus => "+",
        TokenKind.Minus => "-",
        TokenKind.Star => "*",
        TokenKind.Slash => "/",
        TokenKind.DoubleEqual => "==",
        TokenKind.BangEqual => "!=",
        TokenKind.LessThan => "<",
        TokenKind.LessThanEqual => "<=",
        TokenKind.GreaterThan => ">",
        TokenKind.GreaterThanEqual => ">=",
            
        _ => null
    };

    public static TokenKind? GetKeywordKind(ReadOnlySpan<char> text)
    {
        // Short-circuit
        // Shortest keyword is 2 chars (if)
        // Longest keyword is 8 chars (continue)
        if (text.Length is < 2 or > 9)
            return null;
        
        // --- Keyword?
        return text switch
        {
            "break" => TokenKind.BreakKw,
            "continue" => TokenKind.ContinueKw,
            "else" => TokenKind.ElseKw,
            "fun" => TokenKind.FunKw,
            "false" => TokenKind.FalseKw,
            "if" => TokenKind.IfKw,
            "while" => TokenKind.WhileKw,
            "namespace" => TokenKind.NamespaceKw,
            "return" => TokenKind.ReturnKw,
            "true" => TokenKind.TrueKw,
            "using" => TokenKind.UsingKw,
            "var" => TokenKind.VarKw,
            "let" => TokenKind.LetKw,
            "struct" => TokenKind.StructKw,
            "pub" => TokenKind.PubKw,
            "static" => TokenKind.StaticKw,
            "self" => TokenKind.SelfKw,
            "mut" => TokenKind.MutKw,

            _ => null
        };
    }

    public static bool IsKeyword(TokenKind kind)
        => GetText(kind) is { } text && GetKeywordKind(text) is not null;

    public static bool IsWord(TokenKind kind)
        => kind is TokenKind.Identifier or TokenKind.NumberLiteral || IsKeyword(kind);

    public static bool IsTrivia(TokenKind kind)
        => kind is TokenKind.Whitespace or TokenKind.Comment;

    public static bool IsBinaryOperator(TokenKind kind)
        => TryGetInfixPrecedence(kind) is not null;

    public static bool IsUnaryOperator(TokenKind kind)
        => TryGetPrefixPrecedence(kind) is not null;

    public static bool IsComparisonOperator(TokenKind kind)
        => kind is TokenKind.DoubleEqual or TokenKind.BangEqual or TokenKind.LessThan or TokenKind.LessThanEqual
            or TokenKind.GreaterThan or TokenKind.GreaterThanEqual;

    public static bool IsOperator(string text)
        => text is "+" or "-" or "*" or "/" or "!" or "==" or "!=" or "<" or ">" or "<=" or ">=" or "&&" or "||";

    public static bool IsIdentifier(ReadOnlySpan<char> name)
    {
        if (name.Length == 0) return false;
        if (!char.IsAsciiLetter(name[0]) && name[0] is not '_') return false;
        foreach (var c in name)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c is not '_') return false;
        }

        return true;
    }

    public static readonly TokenSet PrefixOperators = TokenSet.Of(TokenKind.Minus, TokenKind.Bang);
    public static readonly TokenSet InfixOperators = TokenSet.Of(TokenKind.Star, TokenKind.Slash, TokenKind.Plus, TokenKind.Minus,
        TokenKind.LessThan, TokenKind.LessThanEqual, TokenKind.GreaterThan, TokenKind.GreaterThanEqual,
        TokenKind.DoubleEqual, TokenKind.BangEqual,
        TokenKind.DoubleAmpersand, TokenKind.DoubleVerticalBar, TokenKind.Equal);
    public static readonly TokenSet PostfixOperators = TokenSet.Of(TokenKind.Dot, TokenKind.OpenParen, TokenKind.OpenBracket);
    
    
    public static Precedence? TryGetPrefixPrecedence(TokenKind kind)
    {
        if (!PrefixOperators.Contains(kind)) return null;
        
        return kind switch
        {
            TokenKind.Minus => Precedence.Negate,
            TokenKind.Bang => Precedence.LogicNot,
            _ => throw new UnreachableException()
        };
    }

    public static Precedence GetPrefixPrecedence(TokenKind kind)
    {
        Guard.MustBe(PrefixOperators.Contains(kind));
        return TryGetPrefixPrecedence(kind)!.Value;
    }

    public static Precedence? TryGetInfixPrecedence(TokenKind kind)
    {
        if (!InfixOperators.Contains(kind)) return null;
        
        return kind switch
        {
            TokenKind.Star or TokenKind.Slash => Precedence.Factor,
            TokenKind.Plus or TokenKind.Minus => Precedence.Sum,

            TokenKind.LessThan or TokenKind.LessThanEqual or TokenKind.GreaterThan or TokenKind.GreaterThanEqual
                or TokenKind.DoubleEqual or TokenKind.BangEqual => Precedence.Comparison,

            TokenKind.DoubleAmpersand => Precedence.LogicAnd,
            TokenKind.DoubleVerticalBar => Precedence.LogicOr,

            TokenKind.Equal => Precedence.Assign,

            _ => throw new UnreachableException()
        };
    }

    public static Precedence GetInfixPrecedence(TokenKind kind)
    {
        Guard.MustBe(InfixOperators.Contains(kind));
        return TryGetInfixPrecedence(kind)!.Value;
    }

    public static Precedence? TryGetPostfixPrecedence(TokenKind kind)
    {
        if (!PostfixOperators.Contains(kind)) return null;
        
        return kind switch
        {
            TokenKind.Dot => Precedence.Dot,
            TokenKind.OpenParen => Precedence.ArgList,
            TokenKind.OpenBracket => Precedence.ArgList,
            _ => throw new UnreachableException(),
        };
    }

    public static Precedence GetPostfixPrecedence(TokenKind kind)
    {
        Guard.MustBe(PostfixOperators.Contains(kind));
        return TryGetPostfixPrecedence(kind)!.Value;
    }
}