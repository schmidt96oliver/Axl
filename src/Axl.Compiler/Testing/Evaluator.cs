using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Testing;

public sealed class Evaluator
{
    private readonly record struct FailedCheck(int LineNumber, string Message)
    {
        public override string ToString() => $"l.{LineNumber}: {Message}";
    }
    
    private readonly record struct DiagnosticId(DiagnosticKind Kind, string Id)
    {
        public override string ToString() => $"{Kind} \'{Id}\'";
    }
    
    
    private readonly List<FailedCheck> _failedChecks = [];
    private readonly TestFile _testFile;

    private Evaluator(TestFile testFile)
    {
        _testFile = testFile;
    }

    public static Evaluation Evaluate(TestFile testFile)
    {
        // Check unsupported compilation diagnostics first, so
        // that erroneous test files are allowed.
        var unsupportedFeatures = testFile.Compilation
            .Diagnostics
            .OfType<Diagnostic.UnsupportedFeature>()
            .ToList();
        if (unsupportedFeatures.Count > 0)
        {
            var message = string.Join('\n', unsupportedFeatures.Select(diagnostic =>
                $"l.{diagnostic.Locations[0].StartLinePosition.Line}: {diagnostic.Message}"));
            return Evaluation.Unsupported(message);
        }
        
        // Test file diagnostics
        if (testFile.Diagnostics.Any())
        {
            var message = "Invalid test file!\n" + string.Join('\n',
                testFile.Diagnostics.Select(diagnostic =>
                    $"l.{diagnostic.Locations[0].StartLinePosition.Line}: {diagnostic.Message}"));
            return Evaluation.Failed(message);
        }
        
        // Unsupported features
        Debug.Assert(testFile.Directive is not null, "Without diagnostics, the test file must have a directive.");
        if (testFile.Directive.Kind is not DirectiveKind.Check)
            return Evaluation.Unsupported($"Test directive {testFile.Directive.Kind} is not supported.");

        if (testFile.Fragments.Count(fragment => !fragment.IsOutput) > 1)
            return Evaluation.Unsupported("Multiple code files are not supported.");

        // Evaluate
        var evaluator = new Evaluator(testFile);
        evaluator.CheckDiagnostics();
        evaluator.CheckTypes();

        if (evaluator._failedChecks.Count > 0)
        {
            var message = string.Join('\n', evaluator._failedChecks);
            return Evaluation.Failed(message);
        }

        return Evaluation.Succeeded;
    }

    
    private void CheckDiagnostics()
    {
        var expected = GetExpectedDiagnostics();
        var actual = GetActualDiagnostics();
        HashSet<int> lines = [.. expected.Keys, .. actual.Keys];
        
        foreach (var line in lines)
        {
            DiagnosticId? expectedId = expected.TryGetValue(line, out var eId) ? eId : null;
            DiagnosticId? actualId = actual.TryGetValue(line, out var aId) ? aId : null;

            if (CheckSingleLineDiagnostic(line, expected: expectedId, got: actualId) is FailedCheck failedCheck)
                _failedChecks.Add(failedCheck);
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

    private Dictionary<int, DiagnosticId> GetExpectedDiagnostics()
    {
        var diagnosticAnnotations = _testFile.Fragments
            .SelectMany(fragment => fragment.Annotations)
            .OfType<DiagnosticAnnotation>();
        
        var table = new Dictionary<int, DiagnosticId>();
        foreach (var annotation in diagnosticAnnotations)
            table.Add(annotation.LineNumber, new DiagnosticId(annotation.Kind, annotation.Id));

        return table;
    }

    private Dictionary<int, DiagnosticId> GetActualDiagnostics()
    {
        var table = new Dictionary<int, DiagnosticId>();
        foreach (var diagnostic in _testFile.Compilation.Diagnostics)
        foreach (var location in diagnostic.Locations)
        {
            var line = location.StartLinePosition.Line;
            if (line != location.EndLinePosition.Line)
            {
                _failedChecks.Add(new FailedCheck(line, $"Unsupported multi-line diagnostic \'{diagnostic.Id}\'"));
                continue;
            }

            var id = new DiagnosticId(diagnostic is Diagnostic.Error ? DiagnosticKind.Error : DiagnosticKind.Lint,
                diagnostic.Id);

            if (!table.TryAdd(line, id))
                _failedChecks.Add(new FailedCheck(line, "Multiple reported diagnostics."));
        }

        return table;
    }
    
    
    private void CheckTypes()
    {
        var typeAnnotations = _testFile.Fragments
            .SelectMany(fragment => fragment.Annotations)
            .OfType<TypeAnnotation>();
        
        foreach (var annotation in typeAnnotations)
        {
            var line = annotation.LineNumber;

            if (_testFile.Compilation.Analysis.SyntaxNodeAt(annotation.ReferencedLocation) is not ExprSyntax syntax)
            {
                _failedChecks.Add(new FailedCheck(line,
                    $"Type annotation does not point to {nameof(ExprSyntax)}"));
                continue;
            }

            if (_testFile.Compilation.Analysis.TypeOf(syntax) is not { } type)
            {
                _failedChecks.Add(new FailedCheck(line, "Could not resolve type"));
                continue;
            }

            if (type.Name != annotation.TypeName)
            {
                _failedChecks.Add(new FailedCheck(line,
                    $"Expected type '{annotation.TypeName}', got '{type.DisplayName}'."));
            }
        }
    }
}