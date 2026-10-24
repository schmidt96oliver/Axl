using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Lowering;

namespace Axl.Compiler.Testing;

public sealed class Evaluator
{
    private readonly record struct FailedCheck(int LineNumber, string Message)
    {
        public override string ToString() => $"l.{LineNumber + 1}: {Message}";
    }

    private enum DiagnosticKind
    {
        Error,
        Lint,
    }
    
    private readonly record struct DiagnosticId(DiagnosticKind Kind, string Id)
    {
        public override string ToString() => $"{Kind} \'{Id}\'";
    }
    
    
    private readonly List<FailedCheck> _failedChecks = [];
    private readonly TestFile _testFile;
    private readonly Compilation _compilation;

    private Evaluator(TestFile testFile, Compilation compilation)
    {
        _testFile = testFile;
        _compilation = compilation;
    }

    public static Evaluation Evaluate(TestFile testFile)
    {
        var compilation = Compilation
            .From(testFile.Fragments.Select(f => f.SourceText))
            .AddBaseLibrary();
        
        // The test file might still contain unsupported features.
        // In that case, tests should be skipped instead of failing.
        var unsupportedFeatures = compilation
            .Diagnostics
            .OfType<Diagnostic.UnsupportedFeature>()
            .ToList();
        if (unsupportedFeatures.Count > 0)
        {
            var message = string.Join('\n', unsupportedFeatures.Select(diagnostic =>
                $"l.{diagnostic.Locations[0].StartLine}: {diagnostic.Message}"));
            return Evaluation.Unsupported(message);
        }

        if (!testFile.Validate(out var errorMessage))
            return Evaluation.Failed($"Invalid taxl file: {errorMessage}");
        
        var evaluator = new Evaluator(testFile, compilation);
        evaluator.CheckDiagnostics();
        switch (testFile.Kind)
        {
            case TestKind.Check:
                break;

            case TestKind.Run:
                evaluator.CheckExpectedOutput();
                break;

            default:
                throw new UnreachableException("Unknown directive.");
        }

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
        var annotations = _testFile.Annotations
            .Where(a => a.Kind is AnnotationKind.ExpectError or AnnotationKind.ExpectLint);
        
        var table = new Dictionary<int, DiagnosticId>();
        foreach (var annotation in annotations)
        {
            var lineNumber = annotation.Prefix.StartLine;
            var kind = annotation.Kind is AnnotationKind.ExpectError ? DiagnosticKind.Error : DiagnosticKind.Lint;
            var id = annotation.Text.Text.Trim().ToString();
            table.Add(lineNumber, new DiagnosticId(kind, id));
        }

        return table;
    }

    private Dictionary<int, DiagnosticId> GetActualDiagnostics()
    {
        var table = new Dictionary<int, DiagnosticId>();
        foreach (var diagnostic in _compilation.Diagnostics)
        {
            if (diagnostic.Locations.Length == 0)
            {
                _failedChecks.Add(new FailedCheck(0, $"Global diagnostic: \'{diagnostic.Id}\'"));
                continue;
            }
            
            foreach (var location in diagnostic.Locations.Select(_testFile.MapToTestFile))
            {
                var line = location.StartLine;
                if (line != location.EndLine)
                {
                    _failedChecks.Add(new FailedCheck(line, $"Multi-line diagnostic \'{diagnostic.Id}\'"));
                    continue;
                }

                var id = new DiagnosticId(diagnostic is Diagnostic.Error ? DiagnosticKind.Error : DiagnosticKind.Lint,
                    diagnostic.Id);

                if (!table.TryAdd(line, id))
                    _failedChecks.Add(new FailedCheck(line, "Multiple reported diagnostics."));
            }
        }

        return table;
    }
    
    
    private void CheckExpectedOutput()
    {
        var expectedOutput = Normalize(_testFile.Annotations
            .Where(a => a.Kind is AnnotationKind.ExpectOutput)
            .Select(a => a.Text.Text.ToString()));
        if (expectedOutput is "")
        {
            _failedChecks.Add(new FailedCheck(0, "Missing expected output."));
            return;
        }

        var output = new StringWriter();
        var compilation = _compilation;
        if (compilation.Scripts.Length != 1)
        {
            _failedChecks.Add(new FailedCheck(0, $"Test file must have 1 script to execute, got {compilation.Scripts.Length}."));
            return;
        }
        
        Miri.Run(new MirProgram(compilation), compilation.Scripts[0].Fun, output);
        
        var actualOutput = Normalize(output.ToString());

        if (actualOutput != expectedOutput)
        {
            var message = $"Output was '{actualOutput}', but expected '{expectedOutput}'.";
            _failedChecks.Add(new FailedCheck(0, message));
        }
    }

    /// <summary>
    /// Removes whitespace that start and end each line and replaces line-breaks
    /// with '\n'. Used for comparing output.
    /// </summary>
    private string Normalize(string input)
        => Normalize(input.Split('\n'));

    private string Normalize(IEnumerable<string> lines)
        => string.Join('\n', lines.Select(line => line.Trim())).Trim();
}