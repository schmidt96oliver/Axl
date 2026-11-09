using System.Collections.Immutable;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;
using AxlLsp.Projects;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace AxlLsp.Handlers;

public sealed class CompletionHandler : CompletionHandlerBase
{
    /// <inheritdoc />
    protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability,
        ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForAxlAndTaxl,
    };

    /// <inheritdoc />
    public override Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        var axlLocation = ProjectStore.TryMapToAxl(request.TextDocument.Uri, request.Position);
        if (axlLocation is null)
            return Task.FromResult(new CompletionList([]));

        var continuationTokens = Parser.ContinuationsAt(axlLocation.Location.SourceText, axlLocation.Location.Start);
        var completionItems = new List<CompletionItem>();
        
        foreach (var kw in continuationTokens.GetKinds().Where(SyntaxFacts.IsKeyword))
            completionItems.Add(Make(kw));
        
        if (continuationTokens.Contains(TokenKind.Identifier))
        {
            var compilation = axlLocation.Compilation;

            var symbols = compilation.SemanticModel.VisibleSymbolsAtUnmapped(axlLocation.Location);
            completionItems.AddRange(symbols
                .GroupBy(s => s.Name)
                .Select(grp => Make(grp.Key, [.. grp])));
        }

        return Task.FromResult(new CompletionList(completionItems));
    }

    private CompletionItem Make(string name, ImmutableArray<Symbol> symbols) => new()
    {
        Label = name,
        Kind = CompletionItemKind.Function,
        //TODO: Improve CompletionItem
        
        // LabelDetails = symbols[0] is FunSymbol fun ?
        //     new CompletionItemLabelDetails
        // {
        //     Detail = "(" +string.Join(", ", fun.Parameters.Select(p => $"{p.Name}: {p.Type.Name}")) + ")",
        //     Description = fun.ReturnType.Name,
        // } : null,
        // Documentation = "DOC COMMENT YAAY",
        // InsertText = symbols[0].Name + "(${0:a}, ${1:b})",
        // InsertTextFormat = InsertTextFormat.Snippet
    };

    private CompletionItem Make(TokenKind keywordKind) => new()
    {
        Label = SyntaxFacts.GetText(keywordKind) ?? "??",
        Kind = CompletionItemKind.Keyword
    };

    /// <inheritdoc />
    public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request);
    }
}