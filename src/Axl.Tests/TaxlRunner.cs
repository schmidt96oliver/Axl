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
    private readonly record struct FailedCheck(int LineNumber, string Message)
    {
        public override string ToString()
            => $"l.{LineNumber}: {Message}";
    }
    
    
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

        // Check diagnostics
        var failedResults = CheckDiagnostics(annotations.OfType<TaxlAnnotation.Diagnostic>(), compilation.Diagnostics)
            .Concat(CheckTypes(taxlFile, compilation, annotations.OfType<TaxlAnnotation.Type>()))
            .ToList();

        if (failedResults.Count > 0)
        {
            var message = string.Join('\n', failedResults);
            TestContext.Current.TestOutputHelper?.WriteLine(message);
            Console.WriteLine(message);
            Assert.Fail("Taxl failed");
        }
    }

    #region Check Diagnostics
    
    private readonly record struct DiagnosticId(DiagnosticKind Kind, string Id)
    {
        public override string ToString()
            => $"{Kind} \'{Id}\'";
    }

    private sealed class DiagnosticsByLine : Dictionary<int, DiagnosticId>;
    
    private static IEnumerable<FailedCheck> CheckDiagnostics(IEnumerable<TaxlAnnotation.Diagnostic> annotations,
        ImmutableArray<Diagnostic> diagnostics)
    {
        var expected = GetDiagnosticsByLine(annotations);
        var actual = GetDiagnosticsByLine(diagnostics);
        HashSet<int> lines = [.. expected.Keys, .. actual.Keys];
        
        foreach (var line in lines)
        {
            DiagnosticId? expectedId = expected.TryGetValue(line, out var eId) ? eId : null;
            DiagnosticId? actualId = actual.TryGetValue(line, out var aId) ? aId : null;

            if (CheckSingleLineDiagnostic(line, expected: expectedId, got: actualId) is FailedCheck failedCheck)
                yield return failedCheck;
        }
    }

    private static FailedCheck? CheckSingleLineDiagnostic(int line, DiagnosticId? expected, DiagnosticId? got)
        => (expected, got) switch
        {
            (null, null) => null,
            (var actualExpected, null) => new(line, $"Expected {actualExpected}, but not reported."),
            (null, var actualGot) => new(line, $"Got {actualGot}, but not expected."),
            var (actualExpected, actualGot) => actualExpected == actualGot
                ? null
                : new(line, $"Reported {actualGot} differs from expected {actualExpected}")
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
    
    #region Types

    private static IEnumerable<FailedCheck> CheckTypes(TaxlFile file, Compilation compilation, IEnumerable<TaxlAnnotation.Type> annotations)
    {
        var sourceFile = file.Source.File;
        foreach (var annotation in annotations)
        {
            var lineNumber = sourceFile.GetLineAt(annotation.AnnotationSpan.First).LineNumber;

            if (annotation.TypeName.Length == 0)
            {
                Assert.Fail($"Type annotation at l.{lineNumber} is empty.");
                throw new UnreachableException();
            }
            
            if (compilation.Analysis.SyntaxNodeAt(new SourceLocation(sourceFile, annotation.ExprSpan)) is not ExprSyntax syntax)
            {
                Assert.Fail($"Type annotation at l.{lineNumber} does not point at {nameof(ExprSyntax)}.");
                throw new UnreachableException();
            }

            if (compilation.Analysis.TypeOf(syntax) is not { } type)
            {
                Assert.Fail($"Type annotation at l.{lineNumber} could not resolve type.");
                throw new UnreachableException();
            }

            if (type.DisplayName != annotation.TypeName)
            {
                yield return new FailedCheck(lineNumber, $"Expected type '{annotation.TypeName}', got '{type.DisplayName}'.");
            }
        }
    }
    
    #endregion
}