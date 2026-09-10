using System.Collections.Concurrent;
using System.Collections.Immutable;
using Axl.Compiler;
using Axl.Compiler.Syntax;
using Axl.Compiler.Taxl;
using Axl.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Axl.Lsp;

public static class DocumentStore
{
    private static readonly ConcurrentDictionary<DocumentUri, Compilation> Compilations = new();
    private static readonly ConcurrentDictionary<DocumentUri, TaxlFile> TaxlFiles = new();
    

    public static void Load(DocumentUri uri, string? text = null)
    {
        try
        {
            var sourceFile = text is null
                ? SourceFile.FromFile(uri.GetFileSystemPath())
                : SourceFile.FromText(uri.GetFileSystemPath(), text);
            
            var isTaxlFile = Path.GetExtension(uri.GetFileSystemPath()) is ".taxl";
            if (isTaxlFile)
            {
                var taxlFile = TaxlFile.Parse(SourceFileView.Whole(sourceFile));
                TaxlFiles[uri] = taxlFile;
                Compilations[uri] = Compilation.From(taxlFile);
            }
            else
            {
                Compilations[uri] = Compilation.From(Parser.Parse(SourceFileView.Whole(sourceFile)));
            }
        }
        catch
        {
            // If an error occurred, just remove the entries.
            Remove(uri);
        }
    }
    
    public static void Remove(DocumentUri uri)
    {
        Compilations.TryRemove(uri, out _);
    }

    
    public static Compilation? GetCompilation(DocumentUri uri)
    {
        if (!Compilations.ContainsKey(uri))
            Load(uri);
        
        return Compilations.GetValueOrDefault(uri);
    }

    public static TaxlFile? TryGetTaxlFile(DocumentUri uri)
    {
        if (!Compilations.ContainsKey(uri))
            Load(uri);

        return TaxlFiles.GetValueOrDefault(uri);
    }
}