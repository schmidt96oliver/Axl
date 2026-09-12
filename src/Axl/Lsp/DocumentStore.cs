using System.Collections.Concurrent;
using Axl.Compiler;
using Axl.Compiler.Syntax;
using Axl.Compiler.Testing;
using Axl.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Axl.Lsp;

public static class DocumentStore
{
    private static readonly ConcurrentDictionary<DocumentUri, Compilation> Compilations = new();
    private static readonly ConcurrentDictionary<DocumentUri, TestFile> TestFiles = new();
    

    public static void Load(DocumentUri uri, string? text = null)
    {
        try
        {
            var sourceText = text is null
                ? SourceText.LoadFile(uri.GetFileSystemPath())
                : SourceText.From(text, fileName: uri.GetFileSystemPath());
            
            var isTaxlFile = Path.GetExtension(uri.GetFileSystemPath()) is ".taxl";
            if (isTaxlFile)
            {
                var testFile = TestFile.From(sourceText);
                TestFiles[uri] = testFile;
                Compilations[uri] = testFile.Compilation;
            }
            else
            {
                Compilations[uri] = Compilation.From(Parser.Parse(sourceText));
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

    public static TestFile? TryGetTestFile(DocumentUri uri)
    {
        if (!Compilations.ContainsKey(uri))
            Load(uri);

        return TestFiles.GetValueOrDefault(uri);
    }
}