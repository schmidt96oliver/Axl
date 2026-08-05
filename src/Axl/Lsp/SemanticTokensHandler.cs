using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Diagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using DiagnosticSeverity = Axl.Compiler.Diagnostics.DiagnosticSeverity;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Axl.Lsp;

public class SemanticTokensHandler(ILanguageServerFacade facade) : SemanticTokensHandlerBase
{
    // Alternation order matters: comments swallow the rest of the line, strings swallow keywords inside them.
    private static readonly Regex TokenRegex = new(
        """(?<decorator>//(?:[@~][a-zA-Z_-]*|-{3,}|={3,}))|(?<comment>//.*)|(?<string>"(?:\\.|[^"\\])*"?)|(?<keyword>\b(?:fn|var|record|module|using|public|private|native|return|if|else|loop|break|continue|and|or|not|true|false|i32|f32|i64|f64|bool|string|none|never|extend|this|ref|value)\b)""",
        RegexOptions.Compiled);

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new SemanticTokensRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(Lsp.LanguageId, Lsp.TestLanguageId),
            Legend = new SemanticTokensLegend
            {
                TokenTypes = new Container<SemanticTokenType>(
                    SemanticTokenType.Comment,
                    SemanticTokenType.String,
                    SemanticTokenType.Keyword,
                    SemanticTokenType.Decorator,
                    SemanticTokenType.Regexp),
                TokenModifiers = []
            },
            Full = true
        };
    }

    protected override Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier,
        CancellationToken cancellationToken)
    {
        var file = DocumentStore.Get(identifier.TextDocument.Uri);
        var diagnosticBag = new DiagnosticBag();
        var tokens = Lexer.Lex(SourceFileView.Whole(file), diagnosticBag);
        
        // --- Push diagnostics
        facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
        {
            Uri = identifier.TextDocument.Uri,
            Diagnostics = new Container<Diagnostic>(ConvertDiagnostics(identifier.TextDocument.Uri, diagnosticBag.Drain()))
        });
        
        // --- Build semantic tokens
        var isInOutput = false;
        foreach (var token in tokens)
        {
            if (token.Span.Length == 0)
                continue;
            if (token.Span.First >= file.Text.Length)
                continue;
            
            var startLinePos = file.GetLinePosition(token.Span.First);
            switch (token.Kind)
            {
                case TokenKind.Comment:
                {
                    var text = file.GetText(token.Span);
                    if (text.StartsWith("//@") || text.StartsWith("//~"))
                    {
                        var length = 3;
                        while (length < text.Length &&
                               (char.IsAsciiLetterOrDigit(text[length]) || text[length] is '_' or '-'))
                        {
                            length++;
                        }
                        builder.Push(startLinePos.Line, startLinePos.Column, length,
                            (SemanticTokenType?)SemanticTokenType.Decorator);
                        
                        // See if there is a comment
                        var commentStart = text[2..].IndexOf("//") + 2;
                        if (commentStart > 2)
                        {
                            builder.Push(startLinePos.Line, startLinePos.Column + commentStart, text.Length - commentStart,
                                (SemanticTokenType?)SemanticTokenType.Comment);
                        }
                    }
                    else if (text.StartsWith("//---") || text.StartsWith("//==="))
                    {
                        var length = 5;
                        while (text[length] is '-' or '=')
                            length++;
                        
                        builder.Push(startLinePos.Line, startLinePos.Column, length,
                            (SemanticTokenType?)SemanticTokenType.Decorator);

                        isInOutput = text.StartsWith("//===");
                    }
                    else if (!isInOutput)
                    {
                        // Entire line is a comment
                        builder.Push(startLinePos.Line, startLinePos.Column, token.Span.Length,
                            (SemanticTokenType?)SemanticTokenType.Comment);
                    }
                    else if (isInOutput)
                    {
                        // `//` is now a decorator
                        builder.Push(startLinePos.Line, startLinePos.Column, 2,
                            (SemanticTokenType?)SemanticTokenType.Decorator);
                        
                        // Everything thereafter is string
                        if (text.Length > 2)
                        {
                            builder.Push(startLinePos.Line, startLinePos.Column + 2, text.Length - 2,
                                (SemanticTokenType?)SemanticTokenType.String);
                        }
                    }
                    break;
                }

                case TokenKind.StringStart:
                case TokenKind.StringEnd:
                    builder.Push(startLinePos.Line, startLinePos.Column, token.Span.Length,
                        (SemanticTokenType?)SemanticTokenType.String);
                    break;
                
                case TokenKind.StringText:
                {
                    // Partition the string text into escape and non-escape
                    var text = file.GetText(token.Span);
                    
                    var stringTokenStart = 0;
                    for (var i = 0; i < text.Length; i++)
                    {
                        if (text[i] is not '\\')
                            continue;

                        // Push string text before
                        if (i > stringTokenStart)
                        {
                            builder.Push(startLinePos.Line,
                                @char: startLinePos.Column + stringTokenStart,
                                length: i - stringTokenStart,
                                (SemanticTokenType?)SemanticTokenType.String);
                        }

                        // Push escape
                        builder.Push(startLinePos.Line,
                            @char: startLinePos.Column + i,
                            length: i + 1 < text.Length ? 2 : 1,
                            (SemanticTokenType?)SemanticTokenType.Regexp);

                        if (i + 1 < text.Length)
                            i++;
                        stringTokenStart = i + 1;
                    }

                    // Push rest string
                    if (text.Length > stringTokenStart)
                    {
                        builder.Push(startLinePos.Line, 
                            @char: startLinePos.Column + stringTokenStart, 
                            length: text.Length - stringTokenStart,
                            (SemanticTokenType?)SemanticTokenType.String);
                    }

                    break;
                }

                case TokenKind.AndKw:
                case TokenKind.BoolKw:
                case TokenKind.BreakKw:
                case TokenKind.ContinueKw:
                case TokenKind.ElseKw:
                case TokenKind.FalseKw:
                case TokenKind.FnKw:
                case TokenKind.IfKw:
                case TokenKind.LoopKw:
                case TokenKind.ModuleKw:
                case TokenKind.NativeKw:
                case TokenKind.NeverKw:
                case TokenKind.NoneKw:
                case TokenKind.NotKw:
                case TokenKind.OrKw:
                case TokenKind.PrivateKw:
                case TokenKind.PublicKw:
                case TokenKind.ReturnKw:
                case TokenKind.StringKw:
                case TokenKind.TrueKw:
                case TokenKind.UsingKw:
                case TokenKind.VarKw:
                case TokenKind.F32Kw:
                case TokenKind.F64Kw:
                case TokenKind.I32Kw:
                case TokenKind.I64Kw:
                    builder.Push(startLinePos.Line, startLinePos.Column, token.Span.Length,
                        (SemanticTokenType?)SemanticTokenType.Keyword);
                    break;
            }
        }
        
        return Task.CompletedTask;
    }

    private IEnumerable<Diagnostic> ConvertDiagnostics(DocumentUri uri, ImmutableArray<Axl.Compiler.Diagnostics.Diagnostic> diagnostics)
    {
        foreach (var diag in diagnostics)
        {
            if (diag.Location.Span.Length == 0)
                continue;
            
            yield return new Diagnostic()
            {
                Severity = diag.DefaultSeverity switch
                {
                    DiagnosticSeverity.Error => OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity
                        .Error,
                    DiagnosticSeverity.Warning => OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity
                        .Warning,
                    _ => OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity.Information
                },
                Message = diag.Hint is null
                    ? diag.Message
                    : $"{diag.Message}\nHint: {diag.Hint}",
                Code = new DiagnosticCode(diag.Id),
                Range = LocationToRange(diag.Location),
                RelatedInformation = new Container<DiagnosticRelatedInformation>(
                    diag.Related.Select(related =>
                        new DiagnosticRelatedInformation()
                        {
                            Location = new Location() { Range = LocationToRange(related.Location), Uri = uri }, //TODO: Get actual URI of SourceFile
                            Message = related.Label
                        })
                ),
            };
        }
    }

    private Range LocationToRange(SourceLocation location)
    {
        var startLinePos = location.GetFirstLinePosition();
        var endLinePos = location.File.GetLinePosition(location.Span.End - 1);
        return new Range(startLinePos.Line, startLinePos.Column, endLinePos.Line, endLinePos.Column + 1);
    }

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));
    }
}