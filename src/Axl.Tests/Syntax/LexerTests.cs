using System.Collections.Immutable;
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;
using Meziantou.Framework.InlineSnapshotTesting;
using Shouldly;

namespace Axl.Tests.Syntax;

public sealed class LexerTests
{
    private ImmutableArray<Token> LexTokens(string input, out ImmutableArray<Diagnostic> diagnostics, out SourceText sourceText)
    {
        var diagnosticBag = new DiagnosticBag();
        sourceText = SourceText.From(input);
        
        var tokens = Lexer.Lex(sourceText, diagnosticBag);
        diagnostics = diagnosticBag.Drain();
        return tokens;
    }
    
    private string All(string input)
    {
        var tokens = LexTokens(input, out var diagnostics, out var sourceText);
        return new Dump(sourceText)
            .Add(diagnostics)
            .Add(tokens, filterTrivia: false)
            .ToString();
    }

    private string NoWhitespace(string input)
    {
        var tokens = LexTokens(input, out var diagnostics, out var sourceText);
        return new Dump(sourceText)
            .Add(diagnostics)
            .Add(tokens.Where(t => t.Kind is not TokenKind.Whitespace), filterTrivia: false)
            .ToString();
    }


    [Fact]
    public void EmptyInput()
        => InlineSnapshot.Validate(All(""), "- Eof");
    
    [Fact]
    public void Whitespace()
        => InlineSnapshot.Validate(All("  \n\r\t   "), """
            - Whitespace: "  \n\r\t   "
            - Eof
            """);

    [Fact]
    public void Comment()
        => InlineSnapshot.Validate(All("""
                                         // Hello
                                         // Second
                                       """), """
            - Whitespace: "  "
            - Comment: "// Hello"
            - Whitespace: "\r\n  "
            - Comment: "// Second"
            - Eof
            """);
    
    [Fact]
    public void InvalidCharacters_Sequence()
        => InlineSnapshot.Validate(All("@@@##"),
            """
            - UnknownCharacters: "@@@##"
            - Eof
            """);

    [Fact]
    public void InvalidCharacters_UnicodeSurrogate()
        => InlineSnapshot.Validate(All("🂦"), """
            - UnknownCharacters: "\uD83C\uDCA6"
            - Eof
            """);

    [Fact]
    public void Keywords()
        => InlineSnapshot.Validate(NoWhitespace("fn var module native return if else while break continue and or not true false i32 f32 i64 f64 bool string char unit using"), """
            - FnKw: "fn"
            - VarKw: "var"
            - ModuleKw: "module"
            - NativeKw: "native"
            - ReturnKw: "return"
            - IfKw: "if"
            - ElseKw: "else"
            - WhileKw: "while"
            - BreakKw: "break"
            - ContinueKw: "continue"
            - AndKw: "and"
            - OrKw: "or"
            - NotKw: "not"
            - TrueKw: "true"
            - FalseKw: "false"
            - I32Kw: "i32"
            - F32Kw: "f32"
            - I64Kw: "i64"
            - F64Kw: "f64"
            - BoolKw: "bool"
            - StringKw: "string"
            - Identifier: "char"
            - UnitKw: "unit"
            - UsingKw: "using"
            - Eof
            """);

    [Fact]
    public void IdentifierVsKeyword()
        => InlineSnapshot.Validate(NoWhitespace("fn vara aif _while else_ false0 False I32"), """
            - FnKw: "fn"
            - Identifier: "vara"
            - Identifier: "aif"
            - Identifier: "_while"
            - Identifier: "else_"
            - Identifier: "false0"
            - Identifier: "False"
            - Identifier: "I32"
            - Eof
            """);

    [Fact]
    public void Never_IsIdentifier()
        => LexTokens("never", out _, out _)
            .ShouldSatisfyAllConditions(
                tokens => tokens.Length.ShouldBe(2),
                tokens => tokens[0].Kind.ShouldBe(TokenKind.Identifier),
                tokens => tokens[1].Kind.ShouldBe(TokenKind.Eof));

    [Fact]
    public void Symbols_Equals()
        => InlineSnapshot.Validate(NoWhitespace("=== != <<=>>= = ++=--="), """
            - DoubleEqual: "=="
            - Equal: "="
            - BangEqual: "!="
            - LessThan: "<"
            - LessThanEqual: "<="
            - GreaterThan: ">"
            - GreaterThanEqual: ">="
            - Equal: "="
            - Plus: "+"
            - PlusEqual: "+="
            - Minus: "-"
            - MinusEqual: "-="
            - Eof
            """);

    [Fact]
    public void Symbols_Other()
        => InlineSnapshot.Validate(NoWhitespace("(){}<>-.,;:"),
            """
            - OpenParen: "("
            - CloseParen: ")"
            - OpenBrace: "{"
            - CloseBrace: "}"
            - LessThan: "<"
            - GreaterThan: ">"
            - Minus: "-"
            - Dot: "."
            - Comma: ","
            - Semicolon: ";"
            - Colon: ":"
            - Eof
            """);


    [Fact]
    public void Numbers_Integral()
        => InlineSnapshot.Validate(NoWhitespace("1234 1_2_3_4 12_ 12_i32 1i64 1_2_f32 1f64"), """
            - NumberLiteral: "1234" block="1234" suffix=None
            - NumberLiteral: "1_2_3_4" block="1234" suffix=None
            - NumberLiteral: "12_" block="12" suffix=None
            - NumberLiteral: "12_i32" block="12" suffix=I32
            - NumberLiteral: "1i64" block="1" suffix=I64
            - NumberLiteral: "1_2_f32" block="12" suffix=F32
            - NumberLiteral: "1f64" block="1" suffix=F64
            - Eof
            """);

    [Fact]
    public void Numbers_WithDot()
        => InlineSnapshot.Validate(NoWhitespace("11.11 .111 1_1_.1_123 1.1_f32 .1_f64 1.1i32 .1111i64"), """
            - NumberLiteral: "11.11" block="11.11" suffix=None
            - NumberLiteral: ".111" block=".111" suffix=None
            - NumberLiteral: "1_1_.1_123" block="11.1123" suffix=None
            - NumberLiteral: "1.1_f32" block="1.1" suffix=F32
            - NumberLiteral: ".1_f64" block=".1" suffix=F64
            - NumberLiteral: "1.1i32" block="1.1" suffix=I32
            - NumberLiteral: ".1111i64" block=".1111" suffix=I64
            - Eof
            """);

    [Fact]
    public void Numbers_BinaryForm()
        => InlineSnapshot.Validate(NoWhitespace("0b1100 0b1_0_1 0b01_"), """
            - NumberLiteral: "0b1100" block="0b1100" suffix=None
            - NumberLiteral: "0b1_0_1" block="0b101" suffix=None
            - NumberLiteral: "0b01_" block="0b01" suffix=None
            - Eof
            """);
    
    [Fact]
    public void Numbers_RejectedBinaryForms()
        => InlineSnapshot.Validate(NoWhitespace("0b1100f32 0b0123 0b 0b_1"), """
            ERROR UnknownNumberSuffix@[18, 19): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'b'.
            ERROR UnknownNumberSuffix@[21, 24): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'b_1'.

            - NumberLiteral: "0b1100" block="0b1100" suffix=None
            - F32Kw: "f32"
            - NumberLiteral: "0b01" block="0b01" suffix=None
            - NumberLiteral: "23" block="23" suffix=None
            - NumberLiteral: "0b" block="0" suffix=None
            - NumberLiteral: "0b_1" block="0" suffix=None
            - Eof
            """);

    [Fact]
    public void Numbers_HexForm()
        => InlineSnapshot.Validate(NoWhitespace("0x0123456789ABCDEFabcdef 0x0_F_F 0x0F_ 0x0Ff32 0x0F_f32"), """
            - NumberLiteral: "0x0123456789ABCDEFabcdef" block="0x0123456789ABCDEFabcdef" suffix=None
            - NumberLiteral: "0x0_F_F" block="0x0FF" suffix=None
            - NumberLiteral: "0x0F_" block="0x0F" suffix=None
            - NumberLiteral: "0x0Ff32" block="0x0Ff32" suffix=None
            - NumberLiteral: "0x0F_f32" block="0x0Ff32" suffix=None
            - Eof
            """);
    
    [Fact]
    public void Numbers_RejectedHexForms()
        => InlineSnapshot.Validate(NoWhitespace("0x0Fi32 0xG 0xh 0xXYZ 0xg 0x 0x_1"), """
            ERROR UnknownNumberSuffix@[9, 11): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'xG'.
            ERROR UnknownNumberSuffix@[13, 15): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'xh'.
            ERROR UnknownNumberSuffix@[17, 21): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'xXYZ'.
            ERROR UnknownNumberSuffix@[23, 25): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'xg'.
            ERROR UnknownNumberSuffix@[27, 28): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'x'.
            ERROR UnknownNumberSuffix@[30, 33): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'x_1'.

            - NumberLiteral: "0x0F" block="0x0F" suffix=None
            - I32Kw: "i32"
            - NumberLiteral: "0xG" block="0" suffix=None
            - NumberLiteral: "0xh" block="0" suffix=None
            - NumberLiteral: "0xXYZ" block="0" suffix=None
            - NumberLiteral: "0xg" block="0" suffix=None
            - NumberLiteral: "0x" block="0" suffix=None
            - NumberLiteral: "0x_1" block="0" suffix=None
            - Eof
            """);
    
    [Fact]
    public void NumberDotIdentifier()
        => InlineSnapshot.Validate(NoWhitespace("1. 1.f32 1.1. 1.1.f32 ._1_1"), """
            - NumberLiteral: "1" block="1" suffix=None
            - Dot: "."
            - NumberLiteral: "1" block="1" suffix=None
            - Dot: "."
            - F32Kw: "f32"
            - NumberLiteral: "1.1" block="1.1" suffix=None
            - Dot: "."
            - NumberLiteral: "1.1" block="1.1" suffix=None
            - Dot: "."
            - F32Kw: "f32"
            - Dot: "."
            - Identifier: "_1_1"
            - Eof
            """);
    
    [Fact]
    public void Numbers_InvalidSuffixes()
        => InlineSnapshot.Validate(NoWhitespace("1f3245 4ghr 1g_445df_12 1f64a 1f644"), """
            ERROR UnknownNumberSuffix@[1, 6): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'f3245'.
            ERROR UnknownNumberSuffix@[8, 11): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'ghr'.
            ERROR UnknownNumberSuffix@[13, 23): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'g_445df_12'.
            ERROR UnknownNumberSuffix@[25, 29): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'f64a'.
            ERROR UnknownNumberSuffix@[31, 35): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'f644'.

            - NumberLiteral: "1f3245" block="1" suffix=None
            - NumberLiteral: "4ghr" block="4" suffix=None
            - NumberLiteral: "1g_445df_12" block="1" suffix=None
            - NumberLiteral: "1f64a" block="1" suffix=None
            - NumberLiteral: "1f644" block="1" suffix=None
            - Eof
            """);


    [Fact]
    public void Strings_Empty()
        => InlineSnapshot.Validate(All("\"\""), """"
            - StringStart: """
            - StringEnd: """
            - Eof
            """");
    
    [Fact]
    public void Strings_Plain()
        => InlineSnapshot.Validate(All("\"Abcdefg //test @.;- f32 fn 🂦🂦 \""), """"
            - StringStart: """
            - StringText: "Abcdefg //test @.;- f32 fn \uD83C\uDCA6\uD83C\uDCA6 " processed="Abcdefg //test @.;- f32 fn 🂦🂦 "
            - StringEnd: """
            - Eof
            """");
    
    [Fact]
    public void Strings_Unclosed()
        => InlineSnapshot.Validate(All("""
                                        "000
                                        "000
                                        """), """"
            - StringStart: """
            - StringText: "000" processed="000"
            - Whitespace: "\r\n"
            - StringStart: """
            - StringText: "000" processed="000"
            - Eof
            """");
    
    [Fact]
    public void Strings_Unclosed_Empty()
        => InlineSnapshot.Validate(All("""
                                       "
                                       ABC
                                       "
                                       """), """"
            - StringStart: """
            - Whitespace: "\r\n"
            - Identifier: "ABC"
            - Whitespace: "\r\n"
            - StringStart: """
            - Eof
            """");
    
    [Fact]
    public void Strings_Escapes()
        => InlineSnapshot.Validate(All("""
                                       "\n \" \{ \} \r \t \\"
                                       """), """"
            - StringStart: """
            - StringText: "\n \" \{ \} \r \t \\" processed="
             " { } 
             	 \"
            - StringEnd: """
            - Eof
            """");

    [Fact]
    public void Strings_UnknownEscapes()
        => InlineSnapshot.Validate(All("""
                                       "\a \ \5 \@ \
                                       "
                                       """), """"
            ERROR UnknownEscapeSequence@[1, 3): Unknown escape sequence '\a'.
            ERROR UnknownEscapeSequence@[4, 6): Unknown escape sequence '\ '.
            ERROR UnknownEscapeSequence@[6, 8): Unknown escape sequence '\5'.
            ERROR UnknownEscapeSequence@[9, 11): Unknown escape sequence '\@'.
            ERROR UnknownEscapeSequence@[12, 13): Unknown escape sequence '\'.

            - StringStart: """
            - StringText: "\a \ \5 \@ \" processed="   "
            - Whitespace: "\r\n"
            - StringStart: """
            - Eof
            """");

    [Fact]
    public void Strings_OpenEscapeBeforeNewlineAndEof()
        => InlineSnapshot.Validate(All("""
                                       "A\
                                       Id
                                       "A\
                                       """), """"
            ERROR UnknownEscapeSequence@[2, 3): Unknown escape sequence '\'.
            ERROR UnknownEscapeSequence@[11, 12): Unknown escape sequence '\'.

            - StringStart: """
            - StringText: "A\" processed="A"
            - Whitespace: "\r\n"
            - Identifier: "Id"
            - Whitespace: "\r\n"
            - StringStart: """
            - StringText: "A\" processed="A"
            - Eof
            """");

    [Fact]
    public void Strings_Interpolated_Basic()
        => InlineSnapshot.Validate(NoWhitespace("""
                                       "a{b}c" "{abc}c" "{1.1 a b}"
                                       """), """"
            - StringStart: """
            - StringText: "a" processed="a"
            - OpenBrace: "{"
            - Identifier: "b"
            - CloseBrace: "}"
            - StringText: "c" processed="c"
            - StringEnd: """
            - StringStart: """
            - OpenBrace: "{"
            - Identifier: "abc"
            - CloseBrace: "}"
            - StringText: "c" processed="c"
            - StringEnd: """
            - StringStart: """
            - OpenBrace: "{"
            - NumberLiteral: "1.1" block="1.1" suffix=None
            - Identifier: "a"
            - Identifier: "b"
            - CloseBrace: "}"
            - StringEnd: """
            - Eof
            """");
    
    [Fact]
    public void Strings_Interpolated_NestedBraces()
        => InlineSnapshot.Validate(NoWhitespace("""
                                       "{ if { } else { { a } a } }"
                                       """), """"
            - StringStart: """
            - OpenBrace: "{"
            - IfKw: "if"
            - OpenBrace: "{"
            - CloseBrace: "}"
            - ElseKw: "else"
            - OpenBrace: "{"
            - OpenBrace: "{"
            - Identifier: "a"
            - CloseBrace: "}"
            - Identifier: "a"
            - CloseBrace: "}"
            - CloseBrace: "}"
            - StringEnd: """
            - Eof
            """");
    
    [Fact]
    public void Strings_Interpolated_NestedInterpolation()
        => InlineSnapshot.Validate(NoWhitespace("""
                                       " { "a" "a { a " b "}" } "
                                       """), """"
            - StringStart: """
            - StringText: " " processed=" "
            - OpenBrace: "{"
            - StringStart: """
            - StringText: "a" processed="a"
            - StringEnd: """
            - StringStart: """
            - StringText: "a " processed="a "
            - OpenBrace: "{"
            - Identifier: "a"
            - StringStart: """
            - StringText: " b " processed=" b "
            - StringEnd: """
            - CloseBrace: "}"
            - StringEnd: """
            - CloseBrace: "}"
            - StringText: " " processed=" "
            - StringEnd: """
            - Eof
            """");

    [Fact]
    public void Strings_InterpolationWithNewline()
        => InlineSnapshot.Validate(All("""
                                       "00{
                                            }00"
                                       """), """"
            - StringStart: """
            - StringText: "00" processed="00"
            - OpenBrace: "{"
            - Whitespace: "\r\n     "
            - CloseBrace: "}"
            - StringText: "00" processed="00"
            - StringEnd: """
            - Eof
            """");

    [Fact]
    public void Strings_UnclosedInterpolation()
        => InlineSnapshot.Validate(All("""
                                       "00{ab 
                                       "00{ab "a {
                                       """), """"
            - StringStart: """
            - StringText: "00" processed="00"
            - OpenBrace: "{"
            - Identifier: "ab"
            - Whitespace: " \r\n"
            - StringStart: """
            - StringText: "00" processed="00"
            - OpenBrace: "{"
            - Identifier: "ab"
            - Whitespace: " "
            - StringStart: """
            - StringText: "a " processed="a "
            - OpenBrace: "{"
            - Eof
            """");

    [Fact]
    public void Strings_CommentInsideInterpolation()
        => InlineSnapshot.Validate(All("""
                                       "00{ab // test}"
                                       """), """"
            - StringStart: """
            - StringText: "00" processed="00"
            - OpenBrace: "{"
            - Identifier: "ab"
            - Whitespace: " "
            - Comment: "// test}""
            - Eof
            """");
}