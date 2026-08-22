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
        static FirstSet()
        {
            Debug.Assert(Enum.GetValues<TokenKind>().All(
                kind => FirstSet.Modifier.Contains(kind) == kind.IsModifier),
                $"{nameof(FirstSet.Modifier)} and {nameof(TokenKindExtensions.get_IsModifier)} out of sync.");    
        }
        
        public static readonly TokenSet NativeTypeName = TokenSet.Of(
            TokenKind.I32Kw, TokenKind.I64Kw, TokenKind.F32Kw, TokenKind.F64Kw, TokenKind.StringKw,
            TokenKind.BoolKw,
            TokenKind.NoneKw
        );

        public static readonly TokenSet QualifiedName = TokenSet.Of(TokenKind.Identifier);

        public static readonly TokenSet TypeName = NativeTypeName | QualifiedName;


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

        public static readonly TokenSet Expr = TailExpr | BodiedExpr;

        public static readonly TokenSet Stmt = Expr | TokenKind.VarKw | TokenKind.UsingKw;

        /// <summary>
        /// `=` as error production.
        /// </summary>
        public static readonly TokenSet Arm = TokenSet.Of(TokenKind.RightDoubleArrow, TokenKind.Equal);

        public static readonly TokenSet Body = Arm | TokenKind.OpenBrace;

        public static readonly TokenSet Modifier = TokenSet.Of(TokenKind.PublicKw, TokenKind.PrivateKw);

        
        public static readonly TokenSet FnDeclAfterModifiers = TokenSet.Of(
            TokenKind.NativeKw, TokenKind.FnKw);

        public static readonly TokenSet FnDecl = Modifier | FnDeclAfterModifiers;

        public static readonly TokenSet ModuleDeclAfterModifiers = TokenSet.Of(TokenKind.ModuleKw);

        public static readonly TokenSet ModuleDecl = Modifier | ModuleDeclAfterModifiers;

        public static readonly TokenSet MemberDecl = FnDecl | ModuleDecl;


        // --- Not FIRST sets, but alternations the ungrammar spells out.

        public static readonly TokenSet AssignOperator = TokenSet.Of(
            TokenKind.Equal, TokenKind.PlusEqual, TokenKind.MinusEqual);

        public static readonly TokenSet StringPart = TokenSet.Of(
            TokenKind.StringStart, TokenKind.StringText, TokenKind.StringEnd);

        /// <summary>
        /// What the Lexer can produce directly after a StringInterpolation while it still
        /// thinks it is inside a string. Anything else means the string is unclosed.
        /// </summary>
        public static readonly TokenSet StringContinuation = TokenSet.Of(
            TokenKind.StringText, TokenKind.StringEnd, TokenKind.OpenBrace);
    }
}