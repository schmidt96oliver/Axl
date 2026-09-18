using System.Diagnostics;

namespace Axl.Compiler.Syntax;

public partial class Parser
{
    /// <summary>
    /// Token sets taken from the ungrammar: FIRST sets of productions, plus the
    /// alternation groups it spells out inline.
    /// </summary>
    /// <remarks>
    /// Every kind in a FIRST set must be dispatched by the matching Eat method, which
    /// throws <see cref="UnreachableException"/> when the set is larger than its switch.
    /// Adding a construct therefore means editing both; a set that was forgotten fails
    /// silently here and is caught by the corpus, not by the parser.
    /// </remarks>
    private static class FirstSet
    {
        public static readonly TokenSet NativeTypeName = TokenSet.Of(
            TokenKind.I32Kw, TokenKind.I64Kw, TokenKind.F32Kw, TokenKind.F64Kw, TokenKind.StringKw,
            TokenKind.BoolKw,
            TokenKind.UnitKw
        );

        public static readonly TokenSet Path = TokenSet.Of(TokenKind.Identifier);

        public static readonly TokenSet TypeName = NativeTypeName | Path;


        public static readonly TokenSet Expr = NativeTypeName | TokenSet.Of(
            TokenKind.TrueKw, TokenKind.FalseKw,
            TokenKind.NumberLiteral,
            TokenKind.Identifier,
            TokenKind.StringStart,
            TokenKind.OpenParen,
            TokenKind.Minus, TokenKind.NotKw,
            
            TokenKind.IfKw, TokenKind.OpenBrace,
            TokenKind.BreakKw, TokenKind.ContinueKw, TokenKind.ReturnKw
        );

        public static readonly TokenSet NonExprStmt = TokenSet.Of(TokenKind.VarKw, TokenKind.WhileKw);
        
        public static readonly TokenSet Stmt = Expr | NonExprStmt;

        
        public static readonly TokenSet FnDecl = TokenSet.Of(TokenKind.FunKw);
        public static readonly TokenSet NativeFnDecl = TokenSet.Of(TokenKind.NativeKw);

        public static readonly TokenSet Member = FnDecl | NativeFnDecl;

        
        public static readonly TokenSet StringPart = TokenSet.Of(
            TokenKind.StringStart, TokenKind.StringText, TokenKind.StringEnd);

        
        public static readonly TokenSet StringContinuation = TokenSet.Of(
            TokenKind.StringText, TokenKind.StringEnd, TokenKind.OpenBrace);
    }
}