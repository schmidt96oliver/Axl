using System.Text.RegularExpressions;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Axl.Lsp;

public class SemanticTokensHandler : SemanticTokensHandlerBase
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
                    SemanticTokenType.Decorator),
                TokenModifiers = []
            },
            Full = true
        };
    }

    protected override Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier,
        CancellationToken cancellationToken)
    {
        var text = DocumentStore.Get(identifier.TextDocument.Uri);
        var lines = text.Split('\n');
        for (var lineNo = 0; lineNo < lines.Length; lineNo++)
        {
            var line = lines[lineNo].TrimEnd('\r');
            foreach (Match match in TokenRegex.Matches(line))
            {
                var type = match.Groups["comment"].Success ? SemanticTokenType.Comment
                    : match.Groups["string"].Success ? SemanticTokenType.String
                    : match.Groups["decorator"].Success ? SemanticTokenType.Decorator
                    : SemanticTokenType.Keyword;
                builder.Push(lineNo, match.Index, match.Length, (SemanticTokenType?)type);
            }
        }

        return Task.CompletedTask;
    }

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));
    }
}