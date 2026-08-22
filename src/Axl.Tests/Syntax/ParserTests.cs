using System.Text;
using Axl.Compiler;
using Axl.Compiler.Syntax;
using Shouldly;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    private static string Tree(string text)
    {
        var source = SourceFileView.FromText(text);
        var tree = Parser.Parse(source);

        return new Dump(source)
            .Add(tree.Diagnostics)
            .AddChildren(tree.Root, filterTrivia: true, filterEof: true)
            .ToString();
    }

    private static string SExpr(string text)
    {
        var source = SourceFileView.FromText(text);
        var tree = Parser.Parse(source);

        var exprStmt = tree.Root.Children[..^1]
            .ShouldHaveSingleItem()
            .ShouldBeAssignableTo<SyntaxNode>();
        exprStmt.Kind.ShouldBe(SyntaxKind.ExprStmt);
        exprStmt.Children.Length.ShouldBeGreaterThan(0);
        var inner = exprStmt.Children[0].ShouldBeAssignableTo<SyntaxNode>();
        
        return new Dump(source)
            .Add(tree.Diagnostics)
            .AddSExpr(inner)
            .ToString();
    }


    [Theory, Corpus]
    public void Corpus_ParsesWithoutDiagnostics(string path)
    {
        var source = SourceFileView.FromFile(path);
        var tree = Parser.Parse(source);

        foreach (var diagnostic in tree.Diagnostics)
        {
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"[{diagnostic.DefaultSeverity}] {diagnostic.Id}: {diagnostic.Message}");
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"    at {path}:line {source.File.GetLineAt(diagnostic.Locations[0].Span.First).LineNumber + 1}");
        }
        
        tree.HasError.ShouldBeFalse();
        tree.Diagnostics.ShouldBeEmpty();
    }

    [Theory, Corpus]
    public void Corpus_ChildrenPartitionTheirParent(string path)
    {
        var source = SourceFileView.FromFile(path);
        var tree = Parser.Parse(source);

        SyntaxWalk.AllNodesRecursive(tree.Root).ShouldAllBe(node => node.FullSpan.IsPartitionedBy(node.Children.Select(child => child.FullSpan)));
    }

    [Theory, Corpus]
    public void Corpus_TokensPartitionSource(string path)
    {
        var source = SourceFileView.FromFile(path);
        var tree = Parser.Parse(source);

        source.Span.IsPartitionedBy(SyntaxWalk.AllTokenSpansRecursive(tree.Root)).ShouldBeTrue();
    }


    /// <inheritdoc cref="CorpusMutations.Prefixes"/>
    [Fact]
    public void PrefixTruncationOnCorpus_KeepsInvariants()
        => CorpusMutations.Check(CorpusMutations.Prefixes, CheckTreeInvariants,
            "A truncated corpus file broke tree invariants.");

    /// <inheritdoc cref="CorpusMutations.TokenDeletions"/>
    [Fact]
    public void TokenDeletionOnCorpus_KeepsInvariants()
        => CorpusMutations.Check(CorpusMutations.TokenDeletions, CheckTreeInvariants,
            "A corpus file with a token deleted broke tree invariants.");

    /// <summary>
    /// Parses <paramref name="text"/> and checks the invariants that must hold for
    /// every input, however broken:
    /// <list type="number">
    /// <item>Parsing does not throw.</item>
    /// <item>Every node's span is partitioned by its children's spans.</item>
    /// <item>The source is partitioned by all token spans.</item>
    /// <item>Concatenating all token texts reproduces the source verbatim.</item>
    /// </list>
    /// </summary>
    /// <returns>Nothing if all invariants hold, otherwise the first violation.</returns>
    private static IEnumerable<Finding> CheckTreeInvariants(string text)
    {
        var source = default(SourceFileView);
        SyntaxTree? tree = null;
        Exception? parseError = null;
        try
        {
            source = SourceFileView.FromText(text);
            tree = Parser.Parse(source);
        }
        catch (Exception e)
        {
            parseError = e;
        }

        if (tree is null)
        {
            yield return new Finding($"(1) Parsing throws {parseError!.GetType().Name}",
                $"Parsing threw {parseError.GetType().Name}: {parseError.Message}");
            yield break;
        }

        foreach (var node in SyntaxWalk.AllNodesRecursive(tree.Root))
        {
            if (node.FullSpan.IsPartitionedBy(node.Children.Select(child => child.FullSpan)))
                continue;

            yield return new Finding("(2) Node is not partitioned by its children",
                $"{node.Kind}@{node.FullSpan} is not partitioned by its children.");
            yield break;
        }

        var tokenSpans = SyntaxWalk.AllTokenSpansRecursive(tree.Root).ToList();
        if (!source.Span.IsPartitionedBy(tokenSpans))
        {
            yield return new Finding("(3) Source is not partitioned by its tokens",
                $"Source@{source.Span} is not partitioned by its {tokenSpans.Count} tokens.");
            yield break;
        }

        var concatenated = new StringBuilder();
        foreach (var span in tokenSpans)
            concatenated.Append(source.GetText(span));

        if (concatenated.ToString() != text)
        {
            yield return new Finding("(4) Token texts do not reproduce the source",
                $"Concatenating all tokens does not reproduce the source. Got:\n{concatenated}");
        }
    }
}