using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Axl.Lsp;

public class FoldingRangeHandler : FoldingRangeHandlerBase
{
    protected override FoldingRangeRegistrationOptions CreateRegistrationOptions(FoldingRangeCapability capability,
        ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(Lsp.LanguageId, Lsp.TestLanguageId)
        };

    public override Task<Container<FoldingRange>?> Handle(FoldingRangeRequestParam request, CancellationToken cancellationToken)
    {
        return Task.FromResult<Container<FoldingRange>?>(null);
    }
}