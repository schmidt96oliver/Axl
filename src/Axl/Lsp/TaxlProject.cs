using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using Axl.Compiler;
using Axl.Compiler.Syntax;
using Axl.Compiler.Testing;
using Axl.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Axl.Lsp;

public sealed class TaxlProject : IProject
{
    public DocumentUri Uri { get; }

    public TestFile TestFile { get; private set; }

    /// <inheritdoc />
    public event Action<UpdatedEvent>? Updated;

    /// <inheritdoc />
    public Compilation Compilation { get; private set; }


    private TaxlProject(DocumentUri uri, TestFile testFile, Compilation compilation)
    {
        Uri = uri;
        TestFile = testFile;
        Compilation = compilation;
        ProjectStore.BaseLibraryProject.Updated += ReloadBaseLib;
    }
    
    public static IProject From(DocumentUri uri, string text)
    {
        var sourceText = SourceText.From(text, uri.GetFileSystemPath());
        var testFile = TestFile.From(sourceText);
        var compilation = Compilation
            .From(testFile.Fragments.Select(f => f.SourceText))
            .AddReference(ProjectStore.BaseLibraryProject.Compilation);
        return new TaxlProject(uri, testFile, compilation);
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
        var testFile = TestFile.From(SourceText.From(text, uri.GetFileSystemPath()));
        TestFile = testFile;
        Compilation = Compilation
            .From(testFile.Fragments.Select(f => f.SourceText))
            .AddReference(ProjectStore.BaseLibraryProject.Compilation);
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
        var inTestFileOrFragment = location.SourceText == TestFile.SourceText ||
                                   TestFile.Fragments.Any(fragment => fragment.SourceText == location.SourceText);
        if (!inTestFileOrFragment)
            return null;

        var locationInTestFile = TestFile.MapToTestFile(location);
        Debug.Assert(locationInTestFile.SourceText == TestFile.SourceText);

        return new Location
        {
            Range = locationInTestFile.GetLspRange(),
            Uri = Uri
        };
    }

    /// <inheritdoc />
    public SourceLocation? TryMapToAxl(Location location)
    {
        if (location.Uri != Uri) 
            return null;

        if (location.Range.TryGetAxlRange(TestFile.SourceText) is not { } rangeInTestFile)
            return null;

        var locationInTestFile = new SourceLocation(TestFile.SourceText, rangeInTestFile);
        var locationInFragment = TestFile.MapToFragment(locationInTestFile);
        return locationInFragment;
    }
}