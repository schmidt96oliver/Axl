namespace Axl.Compiler.Syntax;

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
    
        // --- Assignment Symbols
        TokenKind.Equal => "=",
    
        // --- Bracket Symbols
        TokenKind.OpenParen => "(",
        TokenKind.CloseParen => ")",
        TokenKind.OpenBrace => "{",
        TokenKind.CloseBrace => "}",
    
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

            _ => null
        };
    }

    public static bool IsKeyword(TokenKind kind)
        => GetText(kind) is { } text && GetKeywordKind(text) is not null;

    public static bool IsTrivia(TokenKind kind)
        => kind is TokenKind.Whitespace or TokenKind.Comment;
}