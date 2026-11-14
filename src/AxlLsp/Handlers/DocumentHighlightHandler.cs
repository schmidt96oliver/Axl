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
        if (touchingWord is null)
            return Task.FromResult<DocumentHighlightContainer?>(null);

        var symbol = axlLocation.Compilation.SemanticModel.SymbolFor(touchingWord);
        if (!ShouldShowReference(symbol))
            return Task.FromResult<DocumentHighlightContainer?>(null);

        var references = symbol.Symbols.SelectMany(s =>
                axlLocation.Compilation.SemanticModel.FindReferences(s, scope: axlLocation.SyntaxTree))
            .Where(ShouldShowReference)
            .Select(reference => ProjectStore.TryMapToLsp(reference.ReferenceLocation))
            .Where(lspLocation => lspLocation is not null && lspLocation.Uri == request.TextDocument.Uri)
            .Select(lspLocation => new DocumentHighlight()
            {
                Range = lspLocation!.Range,
                Kind = DocumentHighlightKind.Text,
            })
            .ToList();

        if (references.Count < 2)
        {
            // Only the current touching word (or none) is highlighted, and we can
            // leave that away.
            return Task.FromResult<DocumentHighlightContainer?>(null);
        }

        return Task.FromResult(new DocumentHighlightContainer(references))!;
    }

    private static bool ShouldShowReference(SymbolReference reference) =>
        reference is SymbolReference.Resolved;
}