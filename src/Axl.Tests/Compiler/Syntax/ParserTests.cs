using System.Collections.Immutable;
using AxlCompiler.Syntax;
using AxlCompiler.Testing;
using AxlCompiler.Text;
using Shouldly;

namespace Axl.Tests.Compiler.Syntax;

public partial class ParserTests
{
    private static string Tree(string text)
    {
        var sourceText = SourceText.From(text);
        var tree = Parser.Parse(sourceText);

        return new Dump(sourceText)
            .Add(tree.Diagnostics)
            .AddChildren(tree.FileSyntax, filterTrivia: true, filterEof: true)
            .ToString();
    }

    private static string SExpr(string text)
    {
        var sourceText = SourceText.From(text);
        var tree = Parser.Parse(sourceText);

        var exprStmt = tree.FileSyntax.Children[..^1]
            .ShouldHaveSingleItem()
            .ShouldBeAssignableTo<SyntaxNode>();
        exprStmt.Kind.ShouldBe(SyntaxKind.ExprStmt);
        exprStmt.Children.Length.ShouldBeGreaterThan(0);
        var inner = exprStmt.Children[0].ShouldBeAssignableTo<SyntaxNode>();
        
        return new Dump(sourceText)
            .Add(tree.Diagnostics)
            .AddSExpr(inner)
            .ToString();
    }


    [Theory, Corpus]
    public void Corpus_ParsesWithoutDiagnostics(string path)
    {
        var testFile = TestFile.From(SourceText.LoadFile(path));
        var trees = testFile.Fragments.Select(f => Parser.Parse(f.SourceText)).ToImmutableArray();

        foreach (var diagnostic in trees.SelectMany(t => t.Diagnostics))
        {
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"[{diagnostic.DefaultSeverity}] {diagnostic.Id}: {diagnostic.Message}");

            var mapped = testFile.MapToTestFile(diagnostic.Locations[0]);
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"    at {path}:line {mapped.StartLine + 1}");
        }

        trees.ShouldAllBe(t => !t.HasError);
        trees.ShouldAllBe(t => t.Diagnostics.IsEmpty);
    }

    [Theory, Corpus]
    public void Corpus_ChildrenPartitionTheirParent(string path)
    {
        var sourceText = SourceText.LoadFile(path);
        var tree = Parser.Parse(sourceText);

        tree.FileSyntax.Traverse().OfType<SyntaxNode>().ShouldAllBe(node => node.FullRange.IsPartitionedBy(node.Children.Select(child => child.FullRange)));
    }

    [Theory, Corpus]
    public void Corpus_TokensPartitionSource(string path)
    {
        var sourceText = SourceText.LoadFile(path);
        var tree = Parser.Parse(sourceText);

        sourceText.Range.IsPartitionedBy(tree.FileSyntax.TraverseTokens().Select(t => t.FullRange)).ShouldBeTrue();
    }
}