using AxlLsp.Commands;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace AxlLsp.Handlers;

public class CodeActionHandler : CodeActionHandlerBase
{
    /// <inheritdoc />
    protected override CodeActionRegistrationOptions CreateRegistrationOptions(CodeActionCapability capability,
        ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForAxlAndTaxl,
    };

    /// <inheritdoc />
    public override Task<CommandOrCodeActionContainer?> Handle(CodeActionParams request, CancellationToken cancellationToken)
    {
        var commands = CommandRegistry.Commands.Select( command =>
            (CommandOrCodeAction)new Command { Title = command.Title, Name = command.Identifier });
        return Task.FromResult<CommandOrCodeActionContainer?>(new CommandOrCodeActionContainer(commands));
    }

    /// <inheritdoc />
    public override Task<CodeAction> Handle(CodeAction request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request);
    }
}