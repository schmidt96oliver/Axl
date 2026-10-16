using Axl.Compiler;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Axl.Lsp;

public class TextDocumentSyncHandler(ILanguageServerFacade facade) : TextDocumentSyncHandlerBase
{
    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new(uri, Lsp.LanguageId);

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
        TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TextDocumentSyncRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(Lsp.LanguageId, Lsp.TestLanguageId),
            Change = TextDocumentSyncKind.Full
        };
    }

    public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        ProjectStore.Load(request.TextDocument.Uri, request.TextDocument.Text);
        PushDiagnostics(request.TextDocument.Uri);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        // Full sync: the last content change carries the whole document.
        var text = request.ContentChanges.LastOrDefault()?.Text;
        if (text is not null) 
            ProjectStore.Load(request.TextDocument.Uri, text);
        PushDiagnostics(request.TextDocument.Uri);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        if (request.Text is not null) 
            ProjectStore.Load(request.TextDocument.Uri, request.Text);
        PushDiagnostics(request.TextDocument.Uri);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        ProjectStore.Unload(request.TextDocument.Uri);
        return Unit.Task;
    }


    private void PushDiagnostics(DocumentUri uri)
    {
        var project = ProjectStore.TryGetProject(uri);
        if (project is null) return;
        
        facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = uri,
            Diagnostics = project.GetDiagnostics(uri)
        });
    }
}