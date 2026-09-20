namespace Axl.Compiler.Syntax;

public enum TokenKind
{
    Identifier,
    Comment,
    Whitespace,
    UnknownCharacters,
    Eof,

    // --- Keywords
    FunKw,
    VarKw,
    ModuleKw,
    ReturnKw,
    IfKw,
    ElseKw,
    WhileKw,
    BreakKw,
    ContinueKw,
    AndKw,
    OrKw,
    NotKw,
    UsingKw,
    
    // --- Literals
    NumberLiteral,
    StringStart,
    StringText,
    StringEnd,
    TrueKw,
    FalseKw,
    
    // --- Symbols
    Dot,
    Comma,
    Semicolon,
    Colon,
    
    // --- Assignment Symbols
    Equal,
    
    // --- Bracket Symbols
    OpenParen,
    CloseParen,
    OpenBrace,
    CloseBrace,
    
    // --- Mathematical Symbols
    Plus,
    Minus,
    Star,
    Slash,
    DoubleEqual,
    BangEqual,
    LessThan,
    LessThanEqual,
    GreaterThan,
    GreaterThanEqual,
}

public static class TokenKindExtensions
{
    extension(TokenKind kind)
    {
        public bool IsTrivia => SyntaxFacts.IsTrivia(kind);
        
        public bool HasValue =>
            kind is TokenKind.Identifier or TokenKind.NumberLiteral or TokenKind.StringText;
    }
}