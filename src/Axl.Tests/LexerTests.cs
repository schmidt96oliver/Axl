using System.Text;
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests;

public sealed class LexerTests
{
    private string Lex(string input)
    {
        var diagnostics = new DiagnosticBag();
        var source = SourceFileView.FromText(input);
        var tokens = Lexer.Lex(source, diagnostics);

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
            builder.AppendLine($"- {token.Kind}: \"{text}\"");
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
        => InlineSnapshot.Validate(Lex("never"), """
            - Identifier: "never"
            """);

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
}