using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Axl.Lsp;

internal sealed class AlwaysSerial : IRequestProcessIdentifier
{
    public RequestProcessType Identify(IHandlerDescriptor descriptor) => RequestProcessType.Serial;
}

public static class Lsp
{
    public const string LanguageId = "axl";
    public const string TestLanguageId = "taxl";
    
    public static async Task RunAsync()
    {
        // Run the server
        var server = await LanguageServer.From(options => options
                .WithRequestProcessIdentifier(new AlwaysSerial())
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .WithHandler<TextDocumentSyncHandler>()
                .WithHandler<DocumentDiagnosticHandler>()
                .WithHandler<SemanticTokensHandler>()
                .WithHandler<FoldingRangeHandler>()
                .WithHandler<HoverHandler>()
            );
        await server.WaitForExit;
    }
}