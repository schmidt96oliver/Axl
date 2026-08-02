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
    private ImmutableArray<Token> LexTokens(string input, out DiagnosticBag diagnosticBag, out SourceFileView source)
    {
        diagnosticBag = new DiagnosticBag();
        source = SourceFileView.FromText(input);
        return Lexer.Lex(source, diagnosticBag);
    }
    
    private string Lex(string input)
    {
        var tokens = LexTokens(input, out var diagnostics, out var source);

        return Dump(tokens, source, diagnostics);
    }

    private string LexIgnoreWhitespace(string input)
    {
        var diagnostics = new DiagnosticBag();
        var source = SourceFileView.FromText(input);
        var tokens = Lexer.Lex(source, diagnostics);

        return Dump(tokens.Where(t => t.Kind is not TokenKind.Whitespace), source, diagnostics);
    }

    private string Dump(IEnumerable<Token> tokens, SourceFileView source, DiagnosticBag diagnostics)
    {
        // --- Dump
        var builder = new StringBuilder();

        // Print diagnostics
        if (diagnostics.Diagnostics.Count > 0)
        {
            foreach (var diag in diagnostics.Diagnostics)
                builder.AppendLine(
                    $"{diag.DefaultSeverity.ToString().ToUpper()} {diag.Id}@{diag.Location.Span}: {diag.Message}");
        }
        
        // Print tokens
        foreach (var token in tokens)
        {
            var text = source.GetTextSpan(token.Span).ToLiteralString();
            builder.Append($"- {token.Kind}: \"{text}\"");
            
            if (token is NumberLiteralToken numberLiteral)
            {
                builder.Append($" body=\"{numberLiteral.Body}\" suffix={numberLiteral.Suffix}");
            }

            builder.AppendLine();
        }

        return builder.ToString().Trim();
    }
    
    
    [Fact]
    public void EmptyInput()
        => InlineSnapshot.Validate(Lex(""), "");
    
    [Fact]
    public void Whitespace()
        => InlineSnapshot.Validate(Lex("  \n\r\t   "), """
            - Whitespace: "  \n\r\t   "
            """);
    
    [Fact]
    public void InvalidCharacters_Sequence()
        => InlineSnapshot.Validate(Lex("@@@##"),
            """
            ERROR InvalidCharacters@[0, 1): Invalid characters.
            ERROR InvalidCharacters@[1, 2): Invalid characters.
            ERROR InvalidCharacters@[2, 3): Invalid characters.
            ERROR InvalidCharacters@[3, 4): Invalid characters.
            ERROR InvalidCharacters@[4, 5): Invalid characters.
            - Error: "@@@##"
            """);

    [Fact]
    public void InvalidCharacters_UnicodeSurrogate()
        => InlineSnapshot.Validate(Lex("🂦"), """
            ERROR InvalidCharacters@[0, 1): Invalid characters.
            ERROR InvalidCharacters@[1, 2): Invalid characters.
            - Error: "\uD83C\uDCA6"
            """);

    [Fact]
    public void Keywords()
        => InlineSnapshot.Validate(LexIgnoreWhitespace("fn var module public private native return if else loop break continue and or not true false i32 f32 i64 f64 bool string char none"), """
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
            """);

    [Fact]
    public void Never_IsIdentifier()
        => LexTokens("never", out _, out _)
            .ShouldHaveSingleItem()
            .Kind.ShouldBe(TokenKind.Identifier);

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
            """);
}