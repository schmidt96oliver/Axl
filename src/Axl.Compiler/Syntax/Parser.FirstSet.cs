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
        public static readonly TokenSet Expr = TokenSet.Of(TokenKind.MutKw,
            TokenKind.TrueKw, TokenKind.FalseKw,
            TokenKind.NumberLiteral,
            TokenKind.Identifier,
            TokenKind.SelfKw,
            TokenKind.StringStart,
            TokenKind.OpenParen,
            TokenKind.Minus, TokenKind.Bang,
            
            TokenKind.IfKw, TokenKind.OpenBrace,
            TokenKind.BreakKw, TokenKind.ContinueKw, TokenKind.ReturnKw
        );
        
        public static readonly TokenSet Var = TokenSet.Of(TokenKind.VarKw, TokenKind.LetKw);

        public static readonly TokenSet Annotation = TokenSet.Of(TokenKind.At);

        public static readonly TokenSet NonExprStmt = Var | TokenKind.WhileKw;
        
        public static readonly TokenSet Stmt = Expr | NonExprStmt;

        
        public static readonly TokenSet StringPart = TokenSet.Of(
            TokenKind.StringStart, TokenKind.StringText, TokenKind.StringEnd);

        
        public static readonly TokenSet StringContinuation = TokenSet.Of(
            TokenKind.StringText, TokenKind.StringEnd, TokenKind.OpenBrace);

        public static readonly TokenSet FieldDeclAfterHeader = TokenSet.Of(TokenKind.Identifier);

        public static readonly TokenSet FieldDecl = FieldDeclAfterHeader | TokenKind.PubKw;

        public static readonly TokenSet StructDeclAfterHeader = TokenSet.Of(TokenKind.StructKw);

        public static readonly TokenSet StructDecl = Annotation | TokenKind.PubKw | StructDeclAfterHeader;
        
        public static readonly TokenSet FunDeclAfterHeader =
            TokenSet.Of(TokenKind.StaticKw, TokenKind.MutKw, TokenKind.FunKw);

        public static readonly TokenSet FunDecl = Annotation | TokenKind.PubKw | FunDeclAfterHeader;

        public static readonly TokenSet FunName = TokenSet.Of(TokenKind.Identifier,
            TokenKind.Plus, TokenKind.Minus, TokenKind.Star, TokenKind.Slash,
            TokenKind.LessThan, TokenKind.LessThanEqual, TokenKind.GreaterThan, TokenKind.GreaterThanEqual,
            TokenKind.DoubleEqual, TokenKind.BangEqual, TokenKind.Bang);
    }
}