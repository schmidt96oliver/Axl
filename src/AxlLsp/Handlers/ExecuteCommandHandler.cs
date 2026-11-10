using AxlLsp.Commands;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace AxlLsp.Handlers;

public class ExecuteCommandHandler : ExecuteCommandHandlerBase
{
    /// <inheritdoc />
    protected override ExecuteCommandRegistrationOptions CreateRegistrationOptions(ExecuteCommandCapability capability,
        ClientCapabilities clientCapabilities) => new()
    {
        Commands = new Container<string>(CommandRegistry.Commands.Select(c => c.Identifier))
    };

    /// <inheritdoc />
    public override Task<Unit> Handle(ExecuteCommandParams request, CancellationToken cancellationToken)
    {
        CommandRegistry.TryExecute(request.Command);
        return Task.FromResult(Unit.Value);
    }
}