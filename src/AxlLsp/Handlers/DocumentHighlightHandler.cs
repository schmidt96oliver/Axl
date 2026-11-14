using AxlCompiler;
using AxlLsp.Projects;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace AxlLsp.Handlers;

public class DocumentHighlightHandler : DocumentHighlightHandlerBase
{
    /// <inheritdoc />
    protected override DocumentHighlightRegistrationOptions CreateRegistrationOptions(
        DocumentHighlightCapability capability,
        ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForAxlAndTaxl
    };

    /// <inheritdoc />
    public override Task<DocumentHighlightContainer?> Handle(DocumentHighlightParams request,
        CancellationToken cancellationToken)
    {
        if (ProjectStore.TryMapToAxl(request) is not { } axlLocation)
            return Task.FromResult<DocumentHighlightContainer?>(null);

        var touchingWord = SyntaxContext.At(axlLocation).TouchingWord;
        if (touchingWord is null ||
            axlLocation.Compilation.SemanticModel.SymbolFor(touchingWord) is not SymbolReference.Resolved
            {
                Symbol: var symbol
            })
            return Task.FromResult<DocumentHighlightContainer?>(null);

        var references = axlLocation.Compilation.SemanticModel
            .FindReferences(symbol, scope: axlLocation.SyntaxTree)
            .Select(reference => ProjectStore.TryMapToLsp(reference.ReferenceLocation))
            .Where(lspLocation => lspLocation is not null && lspLocation.Uri == request.TextDocument.Uri)
            .Select(lspLocation => new DocumentHighlight()
            {
                Range = lspLocation!.Range,
                Kind = DocumentHighlightKind.Text,
            });
        
        return Task.FromResult(new DocumentHighlightContainer(references))!;
    }
}