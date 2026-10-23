using System.Collections.Frozen;
using System.Collections.Immutable;
using Axl.Compiler;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Axl.Lsp;

public class BaseLibProject : IProject
{
    private const string PathName = @"C:\Users\schmi\source\Axl\src\BaseLib";

    private FrozenDictionary<DocumentUri, SyntaxTree> _treeByUri;
    

    private BaseLibProject(Compilation compilation, FrozenDictionary<DocumentUri, SyntaxTree> treeByUri)
    {
        _treeByUri = treeByUri;
        Compilation = compilation;
    }

    public static BaseLibProject? TryLoad()
    {
        try
        {
            var allFiles = Directory.EnumerateFiles(PathName, "*.axl", SearchOption.AllDirectories);

            var baseLibCompilation = Compilation.From([
                .. allFiles
                    .Select(SourceText.LoadFile)
                    .Select(Parser.Parse)
            ]);

            var treeByUri = Enumerable.ToDictionary(baseLibCompilation.SyntaxTrees,
                    tree => DocumentUri.FromFileSystemPath(tree.SourceText.FileName!))
                .ToFrozenDictionary();
            return new BaseLibProject(baseLibCompilation, treeByUri);
        }
        catch
        {
            return null;
        }
    }
    
    
    /// <inheritdoc />
    public event Action<UpdatedEvent>? Updated;

    /// <inheritdoc />
    public Compilation Compilation { get; private set; }

    /// <inheritdoc />
    public void Close()
    {
    }

    /// <inheritdoc />
    public bool HasDocument(DocumentUri uri)
    {
        var file = uri.GetFileSystemPath();

        var comparison = OperatingSystem.IsLinux()
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return file.StartsWith(PathName, comparison);
    }
    
    /// <inheritdoc />
    public void UpdateDocument(DocumentUri uri, string? text)
    {
        var allFiles = Directory.EnumerateFiles(PathName, "*.axl", SearchOption.AllDirectories);

        var treesByUri = new Dictionary<DocumentUri, SyntaxTree>();
        
        foreach (var fileName in allFiles)
        {
            var fileUri = DocumentUri.FromFileSystemPath(fileName);
            if (fileUri == uri)
            {
                // Text is given, parse from that
                treesByUri.Add(fileUri, Parser.Parse(text is not null
                    ? SourceText.From(text, fileUri.GetFileSystemPath())
                    : SourceText.LoadFile(fileUri.GetFileSystemPath())));
            }
            else if (_treeByUri.TryGetValue(fileUri, out var tree))
            {
                // Tree is loaded, just reuse it
                treesByUri.Add(fileUri, tree);
            }
            else
            {
                // Tree is not loaded, load it from file system
                treesByUri.Add(fileUri, Parser.Parse(SourceText.LoadFile(fileUri.GetFileSystemPath())));
            }
        }

        var oldCompilation = Compilation;
        var newCompilation = Compilation.From([.. treesByUri.Values]);
        _treeByUri = treesByUri.ToFrozenDictionary();
        Compilation = newCompilation;
        
        Updated?.Invoke(new UpdatedEvent(oldCompilation, newCompilation));
    }
    
    /// <inheritdoc />
    public ImmutableArray<SyntaxTree> GetSyntaxTrees(DocumentUri uri)
        => _treeByUri.TryGetValue(uri, out var tree) ? [tree] : [];

    /// <inheritdoc />
    public Location? TryMapToLsp(SourceLocation location) =>
        _treeByUri.Where(kvp => kvp.Value.SourceText == location.SourceText)
            .Select(kvp => new Location { Uri = kvp.Key, Range = location.GetLspRange() })
            .FirstOrDefault();

    /// <inheritdoc />
    public SourceLocation? TryMapToAxl(Location location)
    {
        if (!_treeByUri.TryGetValue(location.Uri, out var tree))
            return null;
        
        if (location.Range.TryGetAxlRange(tree.SourceText) is not { } range)
            return null;

        return new SourceLocation(tree.SourceText, range);
    }
}