using Axl.Compiler.Symbols;
using Axl.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Axl.Lsp;

public class HoverHandler : HoverHandlerBase
{
    protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability,
        ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(Lsp.LanguageId, Lsp.TestLanguageId)
        };

    public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri;
        var project = ProjectStore.TryGetProject(uri);
        if (project?.TryMapToAxl(uri, request.Position) is not SourceLocation location) 
            return Task.FromResult<Hover?>(null);

        var token = project.Compilation.GetTokenAt(location);
        var symbol = project.Compilation.GetBoundScript(token.Tree).SemanticSideTable.TryGetSymbol(token.Location);
        
        if (symbol is null)
            return Task.FromResult<Hover?>(null);

        var lspLocation = project.TryMapToLsp(token.Location);
        if (lspLocation.Uri != uri)
            return Task.FromResult<Hover?>(null);
        
        return Task.FromResult<Hover?>(new Hover
        {
            Contents = new MarkedStringsOrMarkupContent(symbol.FullDisplayName), 
            Range = lspLocation.Range
        });
    }

}