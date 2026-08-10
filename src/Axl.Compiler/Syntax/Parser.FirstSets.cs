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

        public static readonly TokenSet Expr = TailExpr | TokenSet.Of(TokenKind.OpenBrace,
            TokenKind.LoopKw, TokenKind.IfKw);

        public static readonly TokenSet Stmt = Expr;

        public static readonly TokenSet Modifier = TokenSet.Of(TokenKind.PublicKw, TokenKind.PrivateKw);

        public static readonly TokenSet FnDecl = Modifier | TokenSet.Of(
            TokenKind.NativeKw, TokenKind.FnKw);

        public static readonly TokenSet MemberDecl = FnDecl | TokenSet.Of(
            TokenKind.ModuleKw);
    }
}