using System.Text;
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
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

        AllNodesRecursive(tree.Root).ShouldAllBe(node => node.Span.IsPartitionedBy(node.Children.Select(child => child.Span)));
    }

    [Theory, Corpus]
    public void Corpus_TokensPartitionSource(string path)
    {
        var source = SourceFileView.FromFile(path);
        var tree = Parser.Parse(source);

        source.Span.IsPartitionedBy(AllTokenSpansRecursive(tree.Root)).ShouldBeTrue();
    }


    /// <summary>
    /// Every character prefix of every corpus file is what that file looked like at
    /// some keystroke while it was typed. Character granularity (rather than token)
    /// is deliberate: it is what produces half-formed tokens like an unterminated
    /// string or one half of a "=&gt;".
    /// </summary>
    [Fact]
    public void PrefixTruncationOnCorpus_KeepsInvariants()
        => CheckMutatedCorpus(Prefixes);

    /// <summary>
    /// Deleting a single token models a backspace over a whole word, which is the
    /// other transient state an editing session produces constantly.
    /// </summary>
    [Fact]
    public void TokenDeletionOnCorpus_KeepsInvariants()
        => CheckMutatedCorpus(TokenDeletions);


    private static IEnumerable<(string Label, string Text)> Prefixes(string text)
    {
        for (var length = 0; length <= text.Length; length++)
            yield return ($"prefix of length {length}", text[..length]);
    }

    private static IEnumerable<(string Label, string Text)> TokenDeletions(string text)
    {
        var tokens = Lexer.Lex(SourceFileView.FromText(text), new DiagnosticBag());

        foreach (var token in tokens)
        {
            if (token.Kind.IsTrivia || token.Kind is TokenKind.Eof)
                continue;

            yield return ($"without {token.Kind}@{token.Span}",
                text[..token.Span.First] + text[token.Span.End..]);
        }
    }

    /// <summary>
    /// Runs <paramref name="mutate"/> over every corpus file and asserts
    /// <see cref="CheckTreeInvariants"/> on each mutation. Failing sources are
    /// written verbatim to the test output.
    /// </summary>
    private static void CheckMutatedCorpus(Func<string, IEnumerable<(string Label, string Text)>> mutate)
    {
        // A broken invariant usually breaks for a whole family of mutations at once,
        // so a handful of examples is enough to work with and keeps the output readable.
        const int maxReportedFailures = 10;

        var output = TestContext.Current.TestOutputHelper;
        var caseCount = 0;
        var failureCount = 0;

        var corpusFiles = Directory.EnumerateFiles(CorpusAttribute.Root, "*.taxl", SearchOption.AllDirectories);
        foreach (var path in corpusFiles)
        {
            var name = Path.GetRelativePath(CorpusAttribute.Root, path);

            foreach (var (label, text) in mutate(File.ReadAllText(path)))
            {
                caseCount++;

                if (CheckTreeInvariants(text) is not string violation)
                    continue;

                failureCount++;
                if (failureCount > maxReportedFailures)
                    continue;

                output?.WriteLine($"=== FAILURE {failureCount}: {name}, {label} ===");
                output?.WriteLine(violation);
                output?.WriteLine($"--- source ({text.Length} chars) ---");
                output?.WriteLine(text);
                output?.WriteLine("--- end of source ---");
                output?.WriteLine("");
            }
        }

        output?.WriteLine($"{caseCount} cases, {failureCount} failures.");
        failureCount.ShouldBe(0, "Mutated corpus broke tree invariants. See test output for the failing sources.");
    }

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
    /// <returns><c>null</c> if all invariants hold, otherwise a description of the first violation.</returns>
    private static string? CheckTreeInvariants(string text)
    {
        SourceFileView source;
        SyntaxTree tree;
        try
        {
            source = SourceFileView.FromText(text);
            tree = Parser.Parse(source);
        }
        catch (Exception e)
        {
            return $"(1) Parsing threw {e.GetType().Name}: {e.Message}";
        }

        foreach (var node in AllNodesRecursive(tree.Root))
        {
            if (!node.Span.IsPartitionedBy(node.Children.Select(child => child.Span)))
                return $"(2) {node.Kind}@{node.Span} is not partitioned by its children.";
        }

        var tokenSpans = AllTokenSpansRecursive(tree.Root).ToList();
        if (!source.Span.IsPartitionedBy(tokenSpans))
            return $"(3) Source@{source.Span} is not partitioned by its {tokenSpans.Count} tokens.";

        var concatenated = new StringBuilder();
        foreach (var span in tokenSpans)
            concatenated.Append(source.GetText(span));

        if (concatenated.ToString() != text)
            return $"(4) Concatenating all tokens does not reproduce the source. Got:\n{concatenated}";

        return null;
    }


    /// <summary>
    /// <paramref name="node"/> and all its descendants, parents before children.
    /// </summary>
    private static IEnumerable<SyntaxNode> AllNodesRecursive(SyntaxNode node)
    {
        yield return node;

        foreach (var child in node.Children.OfType<SyntaxNode>())
        foreach (var childNode in AllNodesRecursive(child))
            yield return childNode;
    }

    /// <summary>
    /// Spans of all tokens under <paramref name="element"/>, in document order.
    /// Includes trivia and missing (empty) tokens.
    /// </summary>
    private static IEnumerable<SourceSpan> AllTokenSpansRecursive(SyntaxElement element)
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