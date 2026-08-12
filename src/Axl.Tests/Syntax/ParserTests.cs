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
            .ShouldBeOfType<SyntaxNode>();
        exprStmt.Kind.ShouldBe(SyntaxKind.ExprStmt);
        exprStmt.Children.Length.ShouldBeGreaterThan(0);
        var inner = exprStmt.Children[0].ShouldBeOfType<SyntaxNode>();
        
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

        AllNodesRecursive(tree.Root).ShouldAllBe(node => node.Span.IsPartitionedBy(node.Children.Select(child => child.Span)));
        return;
        
        IEnumerable<SyntaxNode> AllNodesRecursive(SyntaxNode node)
        {
            foreach (var child in node.Children.OfType<SyntaxNode>())
            {
                yield return child;
                foreach (var childNode in AllNodesRecursive(child))
                    yield return childNode;
            }
        }
    }

    [Theory, Corpus]
    public void Corpus_TokensPartitionSource(string path)
    {
        var source = SourceFileView.FromFile(path);
        var tree = Parser.Parse(source);

        source.Span.IsPartitionedBy(AllTokenSpansRecursive(tree.Root)).ShouldBeTrue();
        return;
        
        IEnumerable<SourceSpan> AllTokenSpansRecursive(SyntaxElement element)
        {
            if (element is Token token)
                yield return token.Span;
            else if (element is SyntaxNode node)
            {
                var childTokenSpans = node.Children.SelectMany(AllTokenSpansRecursive);
                foreach (var childTokenSpan in childTokenSpans)
                    yield return childTokenSpan;
            }
        }
    }
}