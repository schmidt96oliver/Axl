using System.Collections.Concurrent;
using System.Collections.Immutable;
using Axl.Compiler;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Axl.Lsp;

public static class DocumentStore
{
    private static readonly ConcurrentDictionary<DocumentUri, SourceFile> Documents = new();
    
    private static readonly ConcurrentDictionary<DocumentUri, ImmutableArray<FileId>> FileIds = new();
    private static readonly ConcurrentDictionary<DocumentUri, Compilation> Compilations = new();


    public static void Load(DocumentUri uri, string? text = null)
    {
        try
        {
            var compilation = text is null
                ? Compilation.FromFile(uri.GetFileSystemPath())
                : Compilation.FromSource(SourceFileView.FromText(text));
            var fileId = compilation.FileIds.First();

            FileIds[uri] = [fileId];
            Compilations[uri] = compilation;
        }
        catch
        {
            // If an error occurred, just remove the entries.
            Remove(uri);
        }
    }
    
    public static void Remove(DocumentUri uri)
    {
        FileIds.TryRemove(uri, out _);
        Compilations.TryRemove(uri, out _);
    }


    public static ImmutableArray<FileId> GetFileIds(DocumentUri uri)
    {
        if (!FileIds.ContainsKey(uri))
            Load(uri);

        if (FileIds.TryGetValue(uri, out var fileIds))
            return fileIds;

        return [];
    }

    public static Compilation? GetCompilation(DocumentUri uri)
    {
        if (!Compilations.ContainsKey(uri))
            Load(uri);
        
        return Compilations.GetValueOrDefault(uri);
    }
}