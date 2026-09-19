namespace Axl.Compiler.Syntax;

public static class SyntaxFacts
{
    public static string? GetText(TokenKind kind) => kind switch
    {
        // --- Keywords
        TokenKind.FunKw => "fun",
        TokenKind.VarKw => "var",
        TokenKind.ModuleKw => "module",
        TokenKind.ReturnKw => "return",
        TokenKind.IfKw => "if",
        TokenKind.ElseKw => "else",
        TokenKind.WhileKw => "while",
        TokenKind.BreakKw => "break",
        TokenKind.ContinueKw => "continue",
        TokenKind.AndKw => "and",
        TokenKind.OrKw => "or",
        TokenKind.NotKw => "not",
        TokenKind.UsingKw => "using",
    
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
        if (text.Length is < 2 or > 8)
            return null;
        
        // --- Keyword?
        return text switch
        {
            "and" => TokenKind.AndKw,
            "break" => TokenKind.BreakKw,
            "continue" => TokenKind.ContinueKw,
            "else" => TokenKind.ElseKw,
            "fun" => TokenKind.FunKw,
            "false" => TokenKind.FalseKw,
            "if" => TokenKind.IfKw,
            "while" => TokenKind.WhileKw,
            "module" => TokenKind.ModuleKw,
            "not" => TokenKind.NotKw,
            "or" => TokenKind.OrKw,
            "return" => TokenKind.ReturnKw,
            "true" => TokenKind.TrueKw,
            "using" => TokenKind.UsingKw,
            "var" => TokenKind.VarKw,

            _ => null
        };
    }

}