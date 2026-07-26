namespace Axl.Compiler.Syntax;

public enum TokenKind
{
    Identifier,
    Comment,
    Whitespace,
    Error,
    
    // --- Keywords
    FnKw,
    VarKw,
    ModuleKw,
    PublicKw,
    PrivateKw,
    NativeKw,
    ReturnKw,
    IfKw,
    ElseKw,
    LoopKw,
    BreakKw,
    ContinueKw,
    AndKw,
    OrKw,
    NotKw,
    
    // --- Type keywords
    I32Kw,
    I64Kw,
    F32Kw,
    F64Kw,
    BoolKw,
    StringKw,
    NoneKw,
    NeverKw,
    
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
    RightArrow,
    RightDoubleArrow,
    
    // --- Assignment Symbols
    Equal,
    PlusEqual,
    MinusEqual,
    
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