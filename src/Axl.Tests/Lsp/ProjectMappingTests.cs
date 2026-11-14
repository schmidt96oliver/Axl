using AxlLsp.Projects;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Shouldly;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Axl.Tests.Lsp;

public sealed class ProjectMappingTests
{
    [Theory, Corpus]
    public void Corpus_Taxl_PositionsRoundTrip(string path)
    {
        var project = TaxlProject.From(DocumentUri.FromFileSystemPath(path), File.ReadAllText(path));
        var uri = DocumentUri.FromFileSystemPath(path);

        var text = project.TestFile.SourceText;

        for (var i = 0; i <= text.Length; i++)
        {
            var line = text.GetLineIndex(i);
            var column = i - text.Lines[line].Start;
            
            var lspLocation = new Location { Uri = uri, Range = new Range(line, column, line, column) };
            var axlLocation = project.TryMapToAxl(lspLocation);
            axlLocation.ShouldNotBeNull();
            
            var backLocation = project.TryMapToLsp(axlLocation.Value);
            backLocation.ShouldNotBeNull();
            backLocation.ShouldBe(lspLocation);
        }
    }
    
    [Theory, Corpus]
    public void Corpus_Taxl_NodeLocationsRoundTrip(string path)
    {
        var project = TaxlProject.From(DocumentUri.FromFileSystemPath(path), File.ReadAllText(path));
        var uri = DocumentUri.FromFileSystemPath(path);

        foreach (var tree in project.GetSyntaxTrees(uri))
        foreach (var node in tree.FileSyntax.Traverse())
        {
            var lspLocation = project.TryMapToLsp(node.Location);
            lspLocation.ShouldNotBeNull();
            
            var axlLocation = project.TryMapToAxl(lspLocation);
            axlLocation.ShouldBe(node.Location);
        }
    }
}