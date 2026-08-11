using System.Diagnostics;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Diagnostics;

public static class TokenKindDisplayExtensions
{
    extension(TokenKind kind)
    {
        /// <summary>
        /// How this kind is named in diagnostic messages. A complete noun phrase,
        /// including quoting: <c>"';'"</c>, <c>"an identifier"</c>. Kinds with a fixed
        /// spelling are quoted, the others are described in prose.
        /// </summary>
        public string DisplayName => kind switch
        {
            TokenKind.Identifier => "an identifier",
            TokenKind.Comment => "a comment",
            TokenKind.Whitespace => "whitespace",
            TokenKind.Error => "an invalid token",
            TokenKind.Eof => "end of file",

            // --- Keywords
            TokenKind.FnKw => "'fn'",
            TokenKind.VarKw => "'var'",
            TokenKind.ModuleKw => "'module'",
            TokenKind.PublicKw => "'public'",
            TokenKind.PrivateKw => "'private'",
            TokenKind.NativeKw => "'native'",
            TokenKind.ReturnKw => "'return'",
            TokenKind.IfKw => "'if'",
            TokenKind.ElseKw => "'else'",
            TokenKind.LoopKw => "'loop'",
            TokenKind.BreakKw => "'break'",
            TokenKind.ContinueKw => "'continue'",
            TokenKind.AndKw => "'and'",
            TokenKind.OrKw => "'or'",
            TokenKind.NotKw => "'not'",
            TokenKind.UsingKw => "'using'",

            // --- Type keywords
            TokenKind.I32Kw => "'i32'",
            TokenKind.I64Kw => "'i64'",
            TokenKind.F32Kw => "'f32'",
            TokenKind.F64Kw => "'f64'",
            TokenKind.BoolKw => "'bool'",
            TokenKind.StringKw => "'string'",
            TokenKind.NoneKw => "'none'",
            TokenKind.NeverKw => "'never'",

            // --- Literals
            TokenKind.NumberLiteral => "a number",
            TokenKind.StringStart => "'\"'",
            TokenKind.StringText => "string text",
            TokenKind.StringEnd => "'\"'",
            TokenKind.TrueKw => "'true'",
            TokenKind.FalseKw => "'false'",

            // --- Symbols
            TokenKind.Dot => "'.'",
            TokenKind.Comma => "','",
            TokenKind.Semicolon => "';'",
            TokenKind.Colon => "':'",
            TokenKind.RightArrow => "'->'",
            TokenKind.RightDoubleArrow => "'=>'",

            // --- Assignment Symbols
            TokenKind.Equal => "'='",
            TokenKind.PlusEqual => "'+='",
            TokenKind.MinusEqual => "'-='",

            // --- Bracket Symbols
            TokenKind.OpenParen => "'('",
            TokenKind.CloseParen => "')'",
            TokenKind.OpenBrace => "'{'",
            TokenKind.CloseBrace => "'}'",

            // --- Mathematical Symbols
            TokenKind.Plus => "'+'",
            TokenKind.Minus => "'-'",
            TokenKind.Star => "'*'",
            TokenKind.Slash => "'/'",
            TokenKind.DoubleEqual => "'=='",
            TokenKind.BangEqual => "'!='",
            TokenKind.LessThan => "'<'",
            TokenKind.LessThanEqual => "'<='",
            TokenKind.GreaterThan => "'>'",
            TokenKind.GreaterThanEqual => "'>='",

            // The switch cannot be exhaustive over an enum, so TokenKindTests
            // walks every declared kind to catch a missing one.
            _ => throw new UnreachableException($"No DisplayName for TokenKind '{kind}'."),
        };
    }
}
