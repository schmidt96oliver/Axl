using System.Collections.Immutable;
using System.Text;
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Meziantou.Framework.InlineSnapshotTesting;
using Shouldly;

namespace Axl.Tests;

public sealed class LexerTests
{
    private ImmutableArray<Token> LexTokens(string input, out ImmutableArray<Diagnostic> diagnostics, out SourceFileView source)
    {
        var diagnosticBag = new DiagnosticBag();
        source = SourceFileView.FromText(input);
        
        var tokens = Lexer.Lex(source, diagnosticBag);
        diagnostics = diagnosticBag.Drain();
        return tokens;
    }
    
    private string Lex(string input)
    {
        var tokens = LexTokens(input, out var diagnostics, out var source);
        return Dump(tokens, source, diagnostics);
    }

    private string LexIgnoreWhitespace(string input)
    {
        var tokens = LexTokens(input, out var diagnostics, out var source);
        return Dump(tokens.Where(t => t.Kind is not TokenKind.Whitespace), source, diagnostics);
    }

    private string Dump(IEnumerable<Token> tokens, SourceFileView source, ImmutableArray<Diagnostic> diagnostics)
    {
        // --- Dump
        var builder = new StringBuilder();

        // Print diagnostics
        foreach (var diag in diagnostics)
            builder.AppendLine(
                $"{diag.DefaultSeverity.ToString().ToUpper()} {diag.Id}@{diag.Location.Span}: {diag.Message.ToLiteralString()}");

        // Print tokens
        foreach (var token in tokens)
        {
            if (token.Kind is TokenKind.Eof)
            {
                builder.AppendLine("- Eof");
                continue;
            }
            
            var text = source.GetText(token.Span).ToLiteralString();
            builder.Append($"- {token.Kind}: \"{text}\"");

            if (token is NumberLiteralToken numberLiteral)
                builder.Append($" body=\"{numberLiteral.Body}\" suffix={numberLiteral.Suffix}");
            else if (token is StringTextToken stringText)
                builder.Append($" processed=\"{stringText.ProcessedText}\"");

            builder.AppendLine();
        }

        return builder.ToString().Trim();
    }


    [Fact]
    public void EmptyInput()
        => InlineSnapshot.Validate(Lex(""), "- Eof");
    
    [Fact]
    public void Whitespace()
        => InlineSnapshot.Validate(Lex("  \n\r\t   "), """
            - Whitespace: "  \n\r\t   "
            - Eof
            """);

    [Fact]
    public void Comment()
        => InlineSnapshot.Validate(Lex("""
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
        => InlineSnapshot.Validate(Lex("@@@##"),
            """
            ERROR UnknownCharacters@[0, 1): Unknown character '@'.
            ERROR UnknownCharacters@[1, 2): Unknown character '@'.
            ERROR UnknownCharacters@[2, 3): Unknown character '@'.
            ERROR UnknownCharacters@[3, 4): Unknown character '#'.
            ERROR UnknownCharacters@[4, 5): Unknown character '#'.
            - Error: "@@@##"
            - Eof
            """);

    [Fact]
    public void InvalidCharacters_UnicodeSurrogate()
        => InlineSnapshot.Validate(Lex("🂦"), """
            ERROR UnknownCharacters@[0, 1): Unknown character '\uD83C'.
            ERROR UnknownCharacters@[1, 2): Unknown character '\uDCA6'.
            - Error: "\uD83C\uDCA6"
            - Eof
            """);

    [Fact]
    public void Keywords()
        => InlineSnapshot.Validate(LexIgnoreWhitespace("fn var module public private native return if else loop break continue and or not true false i32 f32 i64 f64 bool string char none using"), """
            - FnKw: "fn"
            - VarKw: "var"
            - ModuleKw: "module"
            - PublicKw: "public"
            - PrivateKw: "private"
            - NativeKw: "native"
            - ReturnKw: "return"
            - IfKw: "if"
            - ElseKw: "else"
            - LoopKw: "loop"
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
            - NoneKw: "none"
            - UsingKw: "using"
            - Eof
            """);

    [Fact]
    public void IdentifierVsKeyword()
        => InlineSnapshot.Validate(LexIgnoreWhitespace("fn vara aif _private else_ false0 False I32"), """
            - FnKw: "fn"
            - Identifier: "vara"
            - Identifier: "aif"
            - Identifier: "_private"
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
        => InlineSnapshot.Validate(LexIgnoreWhitespace("=== != <<=>>= =>= ++=--="), """
            - DoubleEqual: "=="
            - Equal: "="
            - BangEqual: "!="
            - LessThan: "<"
            - LessThanEqual: "<="
            - GreaterThan: ">"
            - GreaterThanEqual: ">="
            - RightDoubleArrow: "=>"
            - Equal: "="
            - Plus: "+"
            - PlusEqual: "+="
            - Minus: "-"
            - MinusEqual: "-="
            - Eof
            """);

    [Fact]
    public void Symbols_Other()
        => InlineSnapshot.Validate(LexIgnoreWhitespace("(){}<>-->.,;:"),
            """
            - OpenParen: "("
            - CloseParen: ")"
            - OpenBrace: "{"
            - CloseBrace: "}"
            - LessThan: "<"
            - GreaterThan: ">"
            - Minus: "-"
            - RightArrow: "->"
            - Dot: "."
            - Comma: ","
            - Semicolon: ";"
            - Colon: ":"
            - Eof
            """);


    [Fact]
    public void Numbers_Integral()
        => InlineSnapshot.Validate(LexIgnoreWhitespace("1234 1_2_3_4 12_ 12_i32 1i64 1_2_f32 1f64"), """
            - NumberLiteral: "1234" body="1234" suffix=None
            - NumberLiteral: "1_2_3_4" body="1234" suffix=None
            - NumberLiteral: "12_" body="12" suffix=None
            - NumberLiteral: "12_i32" body="12" suffix=I32
            - NumberLiteral: "1i64" body="1" suffix=I64
            - NumberLiteral: "1_2_f32" body="12" suffix=F32
            - NumberLiteral: "1f64" body="1" suffix=F64
            - Eof
            """);

    [Fact]
    public void Numbers_WithDot()
        => InlineSnapshot.Validate(LexIgnoreWhitespace("11.11 .111 1_1_.1_123 1.1_f32 .1_f64 1.1i32 .1111i64"), """
            - NumberLiteral: "11.11" body="11.11" suffix=None
            - NumberLiteral: ".111" body=".111" suffix=None
            - NumberLiteral: "1_1_.1_123" body="11.1123" suffix=None
            - NumberLiteral: "1.1_f32" body="1.1" suffix=F32
            - NumberLiteral: ".1_f64" body=".1" suffix=F64
            - NumberLiteral: "1.1i32" body="1.1" suffix=I32
            - NumberLiteral: ".1111i64" body=".1111" suffix=I64
            - Eof
            """);

    [Fact]
    public void Numbers_BinaryForm()
        => InlineSnapshot.Validate(LexIgnoreWhitespace("0b1100 0b1_0_1 0b01_"), """
            - NumberLiteral: "0b1100" body="0b1100" suffix=None
            - NumberLiteral: "0b1_0_1" body="0b101" suffix=None
            - NumberLiteral: "0b01_" body="0b01" suffix=None
            - Eof
            """);
    
    [Fact]
    public void Numbers_RejectedBinaryForms()
        => InlineSnapshot.Validate(LexIgnoreWhitespace("0b1100f32 0b0123 0b 0b_1"), """
            ERROR UnknownNumberSuffix@[18, 19): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'b'.
            ERROR UnknownNumberSuffix@[21, 24): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'b_1'.
            - NumberLiteral: "0b1100" body="0b1100" suffix=None
            - F32Kw: "f32"
            - NumberLiteral: "0b01" body="0b01" suffix=None
            - NumberLiteral: "23" body="23" suffix=None
            - NumberLiteral: "0b" body="0" suffix=None
            - NumberLiteral: "0b_1" body="0" suffix=None
            - Eof
            """);

    [Fact]
    public void Numbers_HexForm()
        => InlineSnapshot.Validate(LexIgnoreWhitespace("0x0123456789ABCDEFabcdef 0x0_F_F 0x0F_ 0x0Ff32 0x0F_f32"), """
            - NumberLiteral: "0x0123456789ABCDEFabcdef" body="0x0123456789ABCDEFabcdef" suffix=None
            - NumberLiteral: "0x0_F_F" body="0x0FF" suffix=None
            - NumberLiteral: "0x0F_" body="0x0F" suffix=None
            - NumberLiteral: "0x0Ff32" body="0x0Ff32" suffix=None
            - NumberLiteral: "0x0F_f32" body="0x0Ff32" suffix=None
            - Eof
            """);
    
    [Fact]
    public void Numbers_RejectedHexForms()
        => InlineSnapshot.Validate(LexIgnoreWhitespace("0x0Fi32 0xG 0xh 0xXYZ 0xg 0x 0x_1"), """
            ERROR UnknownNumberSuffix@[9, 11): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'xG'.
            ERROR UnknownNumberSuffix@[13, 15): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'xh'.
            ERROR UnknownNumberSuffix@[17, 21): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'xXYZ'.
            ERROR UnknownNumberSuffix@[23, 25): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'xg'.
            ERROR UnknownNumberSuffix@[27, 28): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'x'.
            ERROR UnknownNumberSuffix@[30, 33): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'x_1'.
            - NumberLiteral: "0x0F" body="0x0F" suffix=None
            - I32Kw: "i32"
            - NumberLiteral: "0xG" body="0" suffix=None
            - NumberLiteral: "0xh" body="0" suffix=None
            - NumberLiteral: "0xXYZ" body="0" suffix=None
            - NumberLiteral: "0xg" body="0" suffix=None
            - NumberLiteral: "0x" body="0" suffix=None
            - NumberLiteral: "0x_1" body="0" suffix=None
            - Eof
            """);
    
    [Fact]
    public void NumberDotIdentifier()
        => InlineSnapshot.Validate(LexIgnoreWhitespace("1. 1.f32 1.1. 1.1.f32 ._1_1"), """
            - NumberLiteral: "1" body="1" suffix=None
            - Dot: "."
            - NumberLiteral: "1" body="1" suffix=None
            - Dot: "."
            - F32Kw: "f32"
            - NumberLiteral: "1.1" body="1.1" suffix=None
            - Dot: "."
            - NumberLiteral: "1.1" body="1.1" suffix=None
            - Dot: "."
            - F32Kw: "f32"
            - Dot: "."
            - Identifier: "_1_1"
            - Eof
            """);
    
    [Fact]
    public void Numbers_InvalidSuffixes()
        => InlineSnapshot.Validate(LexIgnoreWhitespace("1f3245 4ghr 1g_445df_12 1f64a 1f644"), """
            ERROR UnknownNumberSuffix@[1, 6): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'f3245'.
            ERROR UnknownNumberSuffix@[8, 11): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'ghr'.
            ERROR UnknownNumberSuffix@[13, 23): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'g_445df_12'.
            ERROR UnknownNumberSuffix@[25, 29): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'f64a'.
            ERROR UnknownNumberSuffix@[31, 35): Only 'i32', 'i64', 'f32' or 'f64' are valid number suffixes. Got 'f644'.
            - NumberLiteral: "1f3245" body="1" suffix=None
            - NumberLiteral: "4ghr" body="4" suffix=None
            - NumberLiteral: "1g_445df_12" body="1" suffix=None
            - NumberLiteral: "1f64a" body="1" suffix=None
            - NumberLiteral: "1f644" body="1" suffix=None
            - Eof
            """);


    [Fact]
    public void Strings_Empty()
        => InlineSnapshot.Validate(Lex("\"\""), """"
            - StringStart: """
            - StringEnd: """
            - Eof
            """");
    
    [Fact]
    public void Strings_Plain()
        => InlineSnapshot.Validate(Lex("\"Abcdefg //test @.;- f32 fn 🂦🂦 \""), """"
            - StringStart: """
            - StringText: "Abcdefg //test @.;- f32 fn \uD83C\uDCA6\uD83C\uDCA6 " processed="Abcdefg //test @.;- f32 fn 🂦🂦 "
            - StringEnd: """
            - Eof
            """");
    
    [Fact]
    public void Strings_Unclosed()
        => InlineSnapshot.Validate(Lex("""
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
        => InlineSnapshot.Validate(Lex("""
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
        => InlineSnapshot.Validate(Lex("""
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
        => InlineSnapshot.Validate(Lex("""
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
        => InlineSnapshot.Validate(Lex("""
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
        => InlineSnapshot.Validate(LexIgnoreWhitespace("""
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
            - NumberLiteral: "1.1" body="1.1" suffix=None
            - Identifier: "a"
            - Identifier: "b"
            - CloseBrace: "}"
            - StringEnd: """
            - Eof
            """");
    
    [Fact]
    public void Strings_Interpolated_NestedBraces()
        => InlineSnapshot.Validate(LexIgnoreWhitespace("""
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
        => InlineSnapshot.Validate(LexIgnoreWhitespace("""
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
        => InlineSnapshot.Validate(Lex("""
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
        => InlineSnapshot.Validate(Lex("""
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
        => InlineSnapshot.Validate(Lex("""
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