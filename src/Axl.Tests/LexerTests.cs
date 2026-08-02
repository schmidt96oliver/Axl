using System.Text;
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Meziantou.Framework.HumanReadable;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests;

public sealed class LexerTests
{
    private string RunLexer(string input)
    {
        // --- Run Lexer
        var diagnostics = new DiagnosticBag();
        var source = SourceFileView.FromText(input);
        var tokens = Lexer.Lex(source, diagnostics);

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
        => InlineSnapshot.Validate(RunLexer(""), "");
    
    [Fact]
    public void Whitespace()
        => InlineSnapshot.Validate(RunLexer("  \n\r\t   "), """
            - Whitespace: "  \n\r\t   "
            """);
    
    [Fact]
    public void InvalidCharacters_Sequence()
        => InlineSnapshot.Validate(RunLexer("@@@##"),
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
        => InlineSnapshot.Validate(RunLexer("🂦"), """
            ERROR InvalidCharacters@[0, 1): Invalid characters.
            ERROR InvalidCharacters@[1, 2): Invalid characters.
            - Error: "\uD83C\uDCA6"
            """);
}