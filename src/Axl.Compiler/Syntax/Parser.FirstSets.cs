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
        
        public static readonly TokenSet TailExpr = OperandExpr | TokenSet.Of(
            TokenKind.BreakKw, TokenKind.ContinueKw, TokenKind.ReturnKw);

        public static readonly TokenSet BodiedExpr = TokenSet.Of(
                TokenKind.IfKw, TokenKind.LoopKw, TokenKind.OpenBrace
        );

        public static readonly TokenSet Expr = TailExpr;

        public static readonly TokenSet Stmt = Expr;
    }
}