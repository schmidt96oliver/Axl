using OmniSharp.Extensions.LanguageServer.Server;

namespace Axl.Lsp;

public static class Lsp
{
    public const string LanguageId = "axl";
    public const string TestLanguageId = "taxl";
    
    public static async Task RunAsync()
    {
        // Run the server
        var server = await LanguageServer.From(options => options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .WithHandler<SemanticTokensHandler>()
                .WithHandler<TextDocumentSyncHandler>()
            );
        await server.WaitForExit;
    }
}