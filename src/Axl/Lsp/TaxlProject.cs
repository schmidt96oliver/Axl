using System.Collections.Immutable;
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
    public Compilation Compilation => TestFile.Compilation;


    private TaxlProject(DocumentUri uri, TestFile testFile)
    {
        Uri = uri;
        TestFile = testFile;
    }
    
    public static IProject From(DocumentUri uri, string text)
    {
        var sourceText = SourceText.From(text, uri.GetFileSystemPath());
        var testFile = TestFile.From(sourceText);
        return new TaxlProject(uri, testFile);
    }


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

    /// <inheritdoc />
    public bool HasDocument(DocumentUri uri)
        => Uri == uri;

    /// <inheritdoc />
    public ImmutableArray<SyntaxTree> GetSyntaxTrees(DocumentUri uri)
        => Uri == uri ? Compilation.SyntaxTrees : [];

    /// <inheritdoc />
    public ImmutableArray<Diagnostic> GetDiagnostics(DocumentUri uri)
        => uri == Uri
            ?
            [
                .. DiagnosticConverter.Convert(Compilation.Diagnostics, uri)
            ]
            : [];
    

    /// <inheritdoc />
    public void UpdateDocument(DocumentUri uri, string text)
    {
        if (uri != Uri) return;

        var testFile = TestFile.From(SourceText.From(text, uri.GetFileSystemPath()));
        TestFile = testFile;
    }
}