using AxlCompiler.Symbols;
using AxlLsp.Projects;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace AxlLsp.Handlers;

public class HoverHandler : HoverHandlerBase
{
    protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability,
        ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = TextDocumentSelector.ForAxlAndTaxl
        };

    public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri;

        if (ProjectStore.TryMapToAxl(uri, request.Position) is not { } axlLocation)
            return Task.FromResult<Hover?>(null);

        var token = axlLocation.Compilation.SyntaxTreeFor(axlLocation.Location.SourceText).TokenAt(axlLocation.Location.Start);
        var symbol = axlLocation.Compilation.SemanticSideTable.TryGetSymbol(token.Location);
        
        if (symbol is null)
            return Task.FromResult<Hover?>(null);

        var lspLocation = ProjectStore.TryMapToLsp(token.Location);
        if (lspLocation is null || lspLocation.Uri != uri)
            return Task.FromResult<Hover?>(null);
        
        return Task.FromResult<Hover?>(new Hover
        {
            Contents = new MarkedStringsOrMarkupContent(symbol.FullDisplayText), 
            Range = lspLocation.Range
        });
    }

}