using System.Collections.Immutable;
using System.Diagnostics;
using AxlCompiler.Diagnostics;
using AxlCompiler.Mir.Interpreter;
using AxlCompiler.Symbols;

namespace AxlCompiler.Testing;

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
    private readonly bool _ignoreGlobalDiagnostics;

    private Evaluator(TestFile testFile)
    {
        _testFile = testFile;

        _ignoreGlobalDiagnostics =
            _testFile.Annotations.Any(a => a.Kind is AnnotationKind.IgnoreGlobalDiagnosticsDirective);
    }

    public static Evaluation Evaluate(TestFile testFile)
    {
        var compilation = Compilation
            .From(testFile.Fragments.Select(f => f.SourceText))
            .WithBaseLibrary();
        
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

        return testFile.Kind switch
        {
            TestKind.Check => EvaluateCheck(testFile, compilation),
            TestKind.Compile => EvaluateCompile(testFile, compilation),
            TestKind.Run => EvaluateRun(testFile, compilation),
            
            _ => throw new UnreachableException($"Unknown {nameof(TestKind)}.{testFile.Kind}.")
        };
    }

    private static Evaluation EvaluateCheck(TestFile testFile, Compilation compilation)
    {
        var evaluator = new Evaluator(testFile);
        evaluator.CheckDiagnostics(compilation.Diagnostics);
        return evaluator.Finish();
    }

    private static Evaluation EvaluateCompile(TestFile testFile, Compilation compilation)
    {
        if (compilation.Scripts is not [var script])
            return Evaluation.Failed("//@compile test must provide exactly one script.");
        
        var evaluator = new Evaluator(testFile);
        var compilationResult = compilation.Compile(script);
        evaluator.CheckDiagnostics(compilationResult.Diagnostics);
        return evaluator.Finish();
    }

    private static Evaluation EvaluateRun(TestFile testFile, Compilation compilation)
    {
        if (compilation.Scripts is not [var script])
            return Evaluation.Failed("//@run test must provide exactly one script.");

        var evaluator = new Evaluator(testFile);
        var compilationResult = compilation.Compile(script);
        evaluator.CheckDiagnostics(compilationResult.Diagnostics);
        
        if (!compilationResult.IsSuccessful)
        {
            evaluator._failedChecks.Add(new FailedCheck(0, "Cannot run test file due to errors."));
            return evaluator.Finish();
        }

        const int branchLimit = 10000;

        
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var result = Miri.Run(compilationResult.Program!,
            new MiriContext(compilation.BaseLibrary.Unit,
                stdout, stderr,
                branchLimit,
                TryGetConstEvaluation: null));

        evaluator.CheckStdOut(stdout.ToString());
        evaluator.CheckRunResult(result, stderr.ToString());
        
        return result switch
        {
            MiriResult.Ok or MiriResult.Panicked => evaluator.Finish(),
            
            MiriResult.HitBranchLimit => Evaluation.Failed("Test hit the branch limit."),
            MiriResult.CannotEvaluateConst => Evaluation.Failed("Test was unable to evaluate a const."),
        };
    }

    
    private Evaluation Finish()
    {
        if (_failedChecks.Count > 0)
        {
            var message = string.Join('\n', _failedChecks);
            return Evaluation.Failed(message);
        }

        return Evaluation.Succeeded;
    }
    
    private void CheckDiagnostics(ImmutableArray<Diagnostic> actualDiagnostics)
    {
        var expected = GetExpectedDiagnostics();
        var actual = GetActualDiagnostics(actualDiagnostics);
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

    private Dictionary<int, DiagnosticId> GetActualDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        var table = new Dictionary<int, DiagnosticId>();
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Locations.Length == 0)
            {
                if (!_ignoreGlobalDiagnostics)
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
    
    
    private void CheckStdOut(string stdout)
    {
        var expectedOutput = Normalize(_testFile.Annotations
            .Where(a => a.Kind is AnnotationKind.ExpectOutput)
            .Select(a => a.Text.Text.ToString()));
        
        var actualOutput = Normalize(stdout);
        if (actualOutput != expectedOutput)
        {
            var message = $"Output was '{actualOutput}', but expected '{expectedOutput}'.";
            _failedChecks.Add(new FailedCheck(0, message));
        }
    }

    private void CheckRunResult(MiriResult result, string stderr)
    {
        var panicAnnotation = _testFile.Annotations.FirstOrDefault(a => a.Kind is AnnotationKind.ExpectPanic);
        var expectPanic = panicAnnotation is not null;
        var gotPanic = result is MiriResult.Panicked;

        if (expectPanic && !gotPanic)
        {
            _failedChecks.Add(new FailedCheck(0, "Expected panic, but did not panic."));
            return;
        }
        
        if (!expectPanic && gotPanic)
        {
            _failedChecks.Add(new FailedCheck(0, $"Unexpected panic. StdErr:\n{stderr}"));
            return;
        }

        if (gotPanic && !stderr.Contains(panicAnnotation!.Text.Text, StringComparison.InvariantCultureIgnoreCase))
        {
            Debug.Assert(expectPanic);

            var message = $"Expected stderr to contain \'{panicAnnotation.Text.Text}\'. Got:\n{stderr}";
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