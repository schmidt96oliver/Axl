using AxlLsp;
using AxlLsp.Handlers;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Server;

var server = await LanguageServer.From(options => options
    .WithRequestProcessIdentifier(new AlwaysSerial())
    .WithInput(Console.OpenStandardInput())
    .WithOutput(Console.OpenStandardOutput())
    .WithHandler<DocumentHandler>()
    .WithHandler<DiagnosticHandler>()
    .WithHandler<SemanticTokensHandler>()
    .WithHandler<HoverHandler>()
    .WithHandler<CompletionHandler>()
);
await server.WaitForExit;



namespace AxlLsp
{
    internal sealed class AlwaysSerial : IRequestProcessIdentifier
    {
        public RequestProcessType Identify(IHandlerDescriptor descriptor) => RequestProcessType.Serial;
    }
}