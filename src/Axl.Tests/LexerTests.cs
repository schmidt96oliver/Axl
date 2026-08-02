using System.Collections.Immutable;
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
        => InlineSnapshot.Validate(LexIgnoreWhitespace("fn var module public private native return if else loop break continue and or not true false i32 f32 i64 f64 bool string char none"));

    [Fact]
    public void IdentifierVsKeyword()
        => InlineSnapshot.Validate(LexIgnoreWhitespace("fn vara aif _private else_ false0 False I32"));
}