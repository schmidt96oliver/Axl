using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Hir;
using Axl.Compiler.Syntax;
using Axl.Compiler.Syntax.Tree;
using Axl.Compiler.Taxl;
using Shouldly;

namespace Axl.Tests;

public static class TaxlRunner
{
    public static void Test(TaxlFile taxlFile)
    {
        var kind = taxlFile.Directives
            .ShouldHaveSingleItem()
            .Kind;
        if (kind is not TaxlDirectiveKind.Check)
            Assert.Skip($"{kind} is not supported yet.");
        
        Check(taxlFile);
    }

    private static void Check(TaxlFile taxlFile)
    {
        var compilation = Compilation.From(taxlFile);
        var unsupportedFeatures = compilation
            .Diagnostics
            .OfType<Diagnostic.UnsupportedFeature>()
            .ToList();
        if (unsupportedFeatures.Count > 0)
        {
            TestContext.Current.TestOutputHelper?.WriteLine(string.Join('\n',
                unsupportedFeatures.Select(diag => diag.Message)));
            Assert.Skip("File has unsupported features.");
        }
        

        var annotations = taxlFile.Fragments
            .OfType<TaxlFragment.Code>()
            .SelectMany(fragment => fragment.Annotations)
            .ToList();

        // Reject invalid annotations
        if (annotations.OfType<TaxlAnnotation.Invalid>().FirstOrDefault() is { } invalidAnnotation)
        {
            Assert.Fail(
                $"Invalid annotation at l.{taxlFile.Source.GetLocation(invalidAnnotation.AnnotationSpan).StartLinePosition.Line}");
        }

        // Skip type annotation
        if (annotations.OfType<TaxlAnnotation.Type>().Any())
            Assert.Skip("Type annotations not supported yet.");
        
        // Check diagnostics
        CheckDiagnostics(annotations.OfType<TaxlAnnotation.Diagnostic>(), compilation.Diagnostics);
    }

    #region Check Diagnostics
    
    private readonly record struct DiagnosticId(DiagnosticKind Kind, string Id)
    {
        public override string ToString()
            => $"{Kind} \'{Id}\'";
    }

    private sealed class DiagnosticsByLine : Dictionary<int, DiagnosticId>;
    
    private static void CheckDiagnostics(IEnumerable<TaxlAnnotation.Diagnostic> annotations,
        ImmutableArray<Diagnostic> diagnostics)
    {
        var expected = GetDiagnosticsByLine(annotations);
        var actual = GetDiagnosticsByLine(diagnostics);
        HashSet<int> lines = [.. expected.Keys, .. actual.Keys];
        
        var comparisons = new Dictionary<int, string?>(expected.Count);
        var fail = false;
        foreach (var line in lines)
        {
            DiagnosticId? expectedId = expected.TryGetValue(line, out var eId) ? eId : null;
            DiagnosticId? actualId = actual.TryGetValue(line, out var aId) ? aId : null;

            var comparisonText = CheckSingleLineDiagnostic(line, expected: expectedId, got: actualId);
            comparisons[line] = comparisonText;
            if (comparisonText is not null)
                fail = true;
        }
        
        var failText = string.Join('\n', comparisons
            .Where(kvp => kvp.Value is not null)
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value));
        TestContext.Current.TestOutputHelper?.WriteLine(failText);
        Console.WriteLine(failText);
        
        if (fail)
            Assert.Fail("Diagnostics do not match.");
    }

    private static string? CheckSingleLineDiagnostic(int line, DiagnosticId? expected, DiagnosticId? got)
        => (expected, got) switch
        {
            (null, null) => null,
            (var actualExpected, null) => $"l.{line} expected {actualExpected}, but not reported.",
            (null, var actualGot) => $"l.{line} got {actualGot}, but not expected.",
            var (actualExpected, actualGot) => actualExpected == actualGot
                ? null
                : $"l.{line} reported {actualGot} differs from expected {actualExpected}"
        };

    private static DiagnosticsByLine GetDiagnosticsByLine(IEnumerable<TaxlAnnotation.Diagnostic> annotations)
    {
        var table = new DiagnosticsByLine();
        foreach (var annotation in annotations)
        {
            if (string.IsNullOrEmpty(annotation.Id))
                Assert.Fail($"File has empty annotations at l.{annotation.LineNumber}.");

            if (table.ContainsKey(annotation.LineNumber))
                Assert.Fail($"File has multiple diagnostic annotations at l.{annotation.LineNumber}.");

            table.Add(annotation.LineNumber, new DiagnosticId(annotation.Kind, annotation.Id));
        }

        return table;
    }

    private static DiagnosticsByLine GetDiagnosticsByLine(ImmutableArray<Diagnostic> diagnostics)
    {
        var table = new DiagnosticsByLine();
        foreach (var diagnostic in diagnostics)
        foreach (var location in diagnostic.Locations)
        {
            var line = location.StartLinePosition.Line;
            if (line != location.EndLinePosition.Line)
            {
                Assert.Fail(
                    $"Unsupported multi-line diagnostic \"{diagnostic.Id}\" at ll.{location.StartLinePosition.Line}-{location.EndLinePosition.Line}");
            }

            var id = new DiagnosticId(
                Kind: diagnostic is Diagnostic.Error ? DiagnosticKind.Error : DiagnosticKind.Lint,
                Id: diagnostic.Id);
            
            if (table.ContainsKey(line))
                Assert.Fail($"Multiple reported diagnostics at l. {line}");
            
            table.Add(line, id);
        }

        return table;
    }
    
    #endregion
    
}