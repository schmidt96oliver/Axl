namespace Axl.Compiler.Syntax;

public partial class Parser
{
    private static class FirstSet
    {
        public static readonly TokenSet NativeTypeName = TokenSet.Of(
            TokenKind.I32Kw, TokenKind.I64Kw, TokenKind.F32Kw, TokenKind.F64Kw, TokenKind.StringKw, TokenKind.NoneKw
        );
        
        public static readonly TokenSet OperandExpr = NativeTypeName | TokenSet.Of(
            TokenKind.TrueKw, TokenKind.FalseKw,
            TokenKind.NumberLiteral,
            TokenKind.Identifier,
            TokenKind.StringStart,
            TokenKind.OpenParen,
            TokenKind.Minus, TokenKind.NotKw
        );
        
        public static readonly TokenSet Expr = OperandExpr;

        public static readonly TokenSet Stmt = Expr;

        public static readonly TokenSet Operator = TokenSet.Of(TokenKind.Plus, TokenKind.Minus, TokenKind.Star,
            TokenKind.Slash, TokenKind.Dot, TokenKind.LessThan, TokenKind.LessThanEqual, TokenKind.GreaterThan,
            TokenKind.GreaterThanEqual, TokenKind.AndKw, TokenKind.OrKw, TokenKind.NotKw, TokenKind.DoubleEqual,
            TokenKind.BangEqual
        );
    }
}