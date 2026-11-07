using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Axl.Lsp;

public sealed class CompletionHandler : CompletionHandlerBase
{
    /// <inheritdoc />
    protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability,
        ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage(Lsp.LanguageId, Lsp.TestLanguageId),
    };

    /// <inheritdoc />
    public override Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        var axlLocation = ProjectStore.TryMapToAxl(request.TextDocument.Uri, request.Position);
        if (axlLocation is null)
            return Task.FromResult(new CompletionList([]));
        
        var compilation = axlLocation.Compilation;

        var symbols = compilation.SemanticModel.VisibleSymbolsAt(axlLocation.Location);
        return Task.FromResult(new CompletionList(symbols
            .GroupBy(s => s.Name)
            .Select(grp => Make(grp.Key, [.. grp]))));
    }

    private CompletionItem Make(string name, ImmutableArray<Symbol> symbols) => new()
    {
        Label = name,
        Kind = CompletionItemKind.Text,
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

    /// <inheritdoc />
    public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request);
    }
}