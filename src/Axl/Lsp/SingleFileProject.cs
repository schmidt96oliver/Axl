using System.Collections.Immutable;
using Axl.Compiler;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Axl.Lsp;

public sealed class SingleFileProject : IProject
{
    public DocumentUri Uri { get; }

    /// <inheritdoc />
    public event Action<UpdatedEvent>? Updated;

    /// <inheritdoc />
    public Compilation Compilation { get; private set; }

    public SourceText SourceText => Compilation.SyntaxTrees.Single().SourceText;


    private SingleFileProject(DocumentUri uri, Compilation compilation)
    {
        Uri = uri;
        Compilation = compilation;
        ProjectStore.BaseLibraryProject.Updated += ReloadBaseLib;
    }
    
    public static SingleFileProject From(DocumentUri uri, string text)
    {
        var sourceText = SourceText.From(text, uri.GetFileSystemPath());
        var compilation = Compilation.From(sourceText);
        return new SingleFileProject(uri, compilation);
    }


    /// <inheritdoc />
    public void Close()
    {
        Updated -= ReloadBaseLib;
    }

    /// <inheritdoc />
    public bool HasDocument(DocumentUri uri)
        => Uri == uri;

    /// <inheritdoc />
    public void UpdateDocument(DocumentUri uri, string? text)
    {
        if (text is null)
            return;
        
        if (uri != Uri) return;
        var oldCompilation = Compilation;
        Compilation = Compilation.From(SourceText.From(text, uri.GetFileSystemPath()));
        Updated?.Invoke(new UpdatedEvent(oldCompilation, Compilation));
    }

    private void ReloadBaseLib(UpdatedEvent baseLibUpdated)
    {
        var oldCompilation = Compilation;
        Compilation = Compilation
            .WithoutReference(baseLibUpdated.OldCompilation)
            .AddReference(baseLibUpdated.NewCompilation);
        Updated?.Invoke(new UpdatedEvent(oldCompilation, Compilation));
    }

    /// <inheritdoc />
    public ImmutableArray<SyntaxTree> GetSyntaxTrees(DocumentUri uri)
        => Uri == uri ? Compilation.SyntaxTrees : [];
    
    
    /// <inheritdoc />
    public Location? TryMapToLsp(SourceLocation location)
    {
        if (location.SourceText != SourceText)
            return null;
        
        return new Location
        {
            Range = location.GetLspRange(),
            Uri = Uri
        };
    }

    /// <inheritdoc />
    public SourceLocation? TryMapToAxl(Location location)
    {
        if (location.Uri != Uri)
            return null;

        if (location.Range.TryGetAxlRange(SourceText) is not { } range)
            return null;
        
        return new SourceLocation(SourceText, range);
    }
}