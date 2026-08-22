using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Shouldly;

namespace Axl.Tests;

/// <summary>
/// One problem found while inspecting a parsed source.
/// </summary>
/// <param name="Key">
/// Groups findings that are the same problem. The report prints each key once with an
/// occurrence count and a single example, so one bug firing on thousands of mutations
/// stays readable. Keep it free of spans, indices and source text.
/// </param>
/// <param name="Detail">Describes this one occurrence, specifics included.</param>
public readonly record struct Finding(string Key, string Detail);

/// <summary>
/// Runs an inspection over every corpus file and over mutations of it.
/// <para>
/// A mutation models a transient state an editing session produces constantly. The
/// corpus is valid input, so anything a mutation breaks is a recovery path — which is
/// exactly the code that is never exercised by hand.
/// </para>
/// </summary>
public static class CorpusMutations
{
    /// <summary>
    /// Every corpus file as (name relative to the corpus root, contents).
    /// </summary>
    public static IEnumerable<(string Name, string Text)> Files()
        => Directory.EnumerateFiles(CorpusAttribute.Root, "*.taxl", SearchOption.AllDirectories)
            .Select(path => (Path.GetRelativePath(CorpusAttribute.Root, path), File.ReadAllText(path)));


    /// <summary>
    /// The file itself, unchanged. For invariants that must already hold on valid input.
    /// </summary>
    public static IEnumerable<(string Label, string Text)> AsIs(string text)
    {
        yield return ("as written", text);
    }

    /// <summary>
    /// Every character prefix of every corpus file is what that file looked like at
    /// some keystroke while it was typed. Character granularity (rather than token)
    /// is deliberate: it is what produces half-formed tokens like an unterminated
    /// string or one half of a "=&gt;".
    /// </summary>
    public static IEnumerable<(string Label, string Text)> Prefixes(string text)
    {
        for (var length = 0; length <= text.Length; length++)
            yield return ($"prefix of length {length}", text[..length]);
    }

    /// <summary>
    /// Deleting a single token models a backspace over a whole word, which is the
    /// other transient state an editing session produces constantly.
    /// </summary>
    public static IEnumerable<(string Label, string Text)> TokenDeletions(string text)
    {
        var tokens = Lexer.Lex(SourceFileView.FromText(text), new DiagnosticBag());

        foreach (var token in tokens)
        {
            if (token.Kind.IsTrivia || token.Kind is TokenKind.Eof)
                continue;

            yield return ($"without {token.Kind}@{token.FullSpan}",
                text[..token.FullSpan.First] + text[token.FullSpan.End..]);
        }
    }


    /// <summary>
    /// Runs <paramref name="mutate"/> over every corpus file, hands each mutation to
    /// <paramref name="inspect"/> and fails if anything was found. Findings are grouped
    /// by <see cref="Finding.Key"/> and written to the test output, newest failure first.
    /// </summary>
    /// <param name="assertMessage">Sentence explaining what the failure means.</param>
    public static void Check(
        Func<string, IEnumerable<(string Label, string Text)>> mutate,
        Func<string, IEnumerable<Finding>> inspect,
        string assertMessage)
    {
        // One broken accessor fires for a whole family of mutations at once. Grouping
        // keeps that to a single entry, so a handful of groups is enough to work with.
        const int maxReportedGroups = 10;

        var output = TestContext.Current.TestOutputHelper;
        var groups = new Dictionary<string, Group>();
        var caseCount = 0;
        var occurrenceCount = 0;

        foreach (var (name, text) in Files())
        foreach (var (label, mutated) in mutate(text))
        {
            caseCount++;

            foreach (var finding in inspect(mutated))
            {
                occurrenceCount++;

                if (groups.TryGetValue(finding.Key, out var group))
                    group.Add(finding.Detail, $"{name}, {label}", mutated);
                else
                    groups[finding.Key] = new Group(finding.Detail, $"{name}, {label}", mutated);
            }
        }

        Report(output, groups, caseCount, occurrenceCount, maxReportedGroups);

        groups.Count.ShouldBe(0,
            $"{assertMessage} {groups.Count} distinct finding(s) in {occurrenceCount} occurrence(s). " +
            "See test output for details and the failing sources.");
    }


    private static void Report(ITestOutputHelper? output,
        Dictionary<string, Group> groups,
        int caseCount,
        int occurrenceCount,
        int maxReportedGroups)
    {
        if (output is null)
            return;

        if (groups.Count == 0)
        {
            output.WriteLine($"{caseCount} cases, no findings.");
            return;
        }

        output.WriteLine($"{caseCount} cases, {groups.Count} distinct finding(s), " +
                         $"{occurrenceCount} occurrence(s).");
        output.WriteLine("");

        var ranked = groups
            .OrderByDescending(group => group.Value.Occurrences)
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .ToList();

        foreach (var (index, (key, group)) in ranked.Take(maxReportedGroups).Index())
        {
            output.WriteLine($"=== [{index + 1}] {key} ===");
            output.WriteLine($"    {group.Occurrences} occurrence(s)");
            output.WriteLine($"    {group.Detail}");
            output.WriteLine($"    smallest case: {group.Where}");
            output.WriteLine($"    --- source ({group.Source.Length} chars) ---");
            output.WriteLine(group.Source);
            output.WriteLine("    --- end of source ---");
            output.WriteLine("");
        }

        if (ranked.Count > maxReportedGroups)
        {
            output.WriteLine($"... and {ranked.Count - maxReportedGroups} more distinct finding(s):");
            foreach (var (key, group) in ranked.Skip(maxReportedGroups))
                output.WriteLine($"    {group.Occurrences,8}x  {key}");
        }
    }


    /// <summary>
    /// All occurrences of one <see cref="Finding.Key"/>, plus the smallest one verbatim.
    /// Smallest rather than first, because with <see cref="Prefixes"/> the shortest
    /// failing source is the minimal reproduction.
    /// </summary>
    private sealed class Group(string detail, string where, string source)
    {
        public int Occurrences { get; private set; } = 1;
        public string Detail { get; private set; } = detail;
        public string Where { get; private set; } = where;
        public string Source { get; private set; } = source;

        public void Add(string detail, string where, string source)
        {
            Occurrences++;

            if (source.Length >= Source.Length)
                return;

            Detail = detail;
            Where = where;
            Source = source;
        }
    }
}
