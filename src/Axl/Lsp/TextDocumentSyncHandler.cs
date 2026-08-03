using System.Collections.Concurrent;
using Axl.Compiler;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Axl.Lsp;

public static class DocumentStore
{
    private static readonly ConcurrentDictionary<DocumentUri, SourceFile> Documents = new();

    public static void Set(DocumentUri uri, SourceFile file) => Documents[uri] = file;

    public static void Remove(DocumentUri uri) => Documents.TryRemove(uri, out _);

    public static SourceFile Get(DocumentUri uri)
    {
        if (Documents.TryGetValue(uri, out var file)) 
            return file;
        try
        {
            var readFile = SourceFile.FromFile(uri.GetFileSystemPath());
            Set(uri, readFile);
            return readFile;
        }
        catch
        {
            return SourceFile.FromText("");
        }
    }
}

public class TextDocumentSyncHandler : TextDocumentSyncHandlerBase
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
        DocumentStore.Set(request.TextDocument.Uri, SourceFile.FromText(request.TextDocument.Uri.GetFileSystemPath(), request.TextDocument.Text));
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        // Full sync: the last content change carries the whole document.
        var text = request.ContentChanges.LastOrDefault()?.Text;
        if (text is not null) 
            DocumentStore.Set(request.TextDocument.Uri, SourceFile.FromText(request.TextDocument.Uri.GetFileSystemPath(), text));
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        if (request.Text is not null) 
            DocumentStore.Set(request.TextDocument.Uri, SourceFile.FromText(request.TextDocument.Uri.GetFileSystemPath(), request.Text));
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        DocumentStore.Remove(request.TextDocument.Uri);
        return Unit.Task;
    }
}