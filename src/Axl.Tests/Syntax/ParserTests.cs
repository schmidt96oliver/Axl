using System.Text;
using Axl.Compiler;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;
using Shouldly;

namespace Axl.Tests.Syntax;

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
        var sourceText = SourceText.LoadFile(path);
        var tree = Parser.Parse(sourceText);

        foreach (var diagnostic in tree.Diagnostics)
        {
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"[{diagnostic.DefaultSeverity}] {diagnostic.Id}: {diagnostic.Message}");
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"    at {path}:line {sourceText.GetLineIndex(diagnostic.Locations[0].Range.Start) + 1}");
        }
        
        tree.HasError.ShouldBeFalse();
        tree.Diagnostics.ShouldBeEmpty();
    }

    [Theory, Corpus]
    public void Corpus_ChildrenPartitionTheirParent(string path)
    {
        var sourceText = SourceText.LoadFile(path);
        var tree = Parser.Parse(sourceText);

        AllNodesRecursive(tree.FileSyntax).ShouldAllBe(node => node.FullRange.IsPartitionedBy(node.Children.Select(child => child.FullRange)));
    }

    [Theory, Corpus]
    public void Corpus_TokensPartitionSource(string path)
    {
        var sourceText = SourceText.LoadFile(path);
        var tree = Parser.Parse(sourceText);

        sourceText.Range.IsPartitionedBy(AllTokensRecursive(tree.FileSyntax).Select(t => t.FullRange)).ShouldBeTrue();
    }
    
    
    public static IEnumerable<SyntaxNode> AllNodesRecursive(SyntaxNode node)
    {
        yield return node;

        foreach (var child in node.Children.OfType<SyntaxNode>())
        foreach (var childNode in AllNodesRecursive(child))
            yield return childNode;
    }

    public static IEnumerable<Token> AllTokensRecursive(SyntaxElement element)
    {
        if (element is Token token)
            yield return token;
        else if (element is SyntaxNode node)
        {
            foreach (var child in node.Children)
            foreach (var childTokens in AllTokensRecursive(child))
                yield return childTokens;
        }
    }
}