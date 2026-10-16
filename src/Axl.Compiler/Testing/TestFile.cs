using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public enum TestKind
{
    /// <summary>
    /// The test file contains no directive, or it
    /// could not be recognized.
    /// </summary>
    Error,

    Check,
    Run,
}

public sealed class TestFile
{
    public TestKind Kind { get; }

    public SourceText SourceText { get; }
    
    public ImmutableArray<Fragment> Fragments { get; }

    public ImmutableArray<Annotation> Annotations { get; }


    public Compilation Compilation
    {
        get
        {
            field ??= Compilation.From(Fragments.Select(frag => frag.SourceText));
            return field;
        }
    }

    private Evaluation? _evaluation;

    public Evaluation Evaluation
    {
        get
        {
            _evaluation ??= Evaluator.Evaluate(this);
            return _evaluation.Value;
        }
    }


    private TestFile(SourceText sourceText, 
        TestKind kind,
        ImmutableArray<Fragment> fragments,
        ImmutableArray<Annotation> annotations)
    {
        SourceText = sourceText;
        Kind = kind;
        Fragments = fragments;
        Annotations = annotations;
    }


    public static TestFile From(SourceText sourceText)
    {
        var annotations = ParseAnnotations(sourceText);
        var fragments = ParseFragments(sourceText);

        var kind = annotations.FirstOrDefault(a =>
                a.Kind is AnnotationKind.CheckDirective or AnnotationKind.RunDirective) switch
            {
                { Kind: AnnotationKind.CheckDirective } => TestKind.Check,
                { Kind: AnnotationKind.RunDirective } => TestKind.Run,
                _ => TestKind.Error
            };

        return new TestFile(sourceText, kind, fragments, annotations);
    }


    /// <summary>
    /// Returns <paramref name="location"/> relative to <see cref="SourceText"/>.
    /// Expects that <paramref name="location"/> refers to this test file or
    /// one of its fragments.
    /// </summary>
    public SourceLocation MapToTestFile(SourceLocation location)
    {
        if (ReferenceEquals(location.SourceText, SourceText))
            return location;

        var fragment = Fragments.SingleOrDefault(frag => frag.SourceText == location.SourceText &&
                                                         frag.SourceText.Range.Contains(location.Range))
                       ?? throw new ArgumentException(
                           $"The given {nameof(location)} does not reference this {nameof(TestFile)} or a fragment thereof.",
                           nameof(location));
        var range = SourceRange.FromLength(location.Start + fragment.Location.Start, location.Length);
        return new SourceLocation(SourceText, range);
    }

    /// <summary>
    /// Returns the given <paramref name="location"/> relative to the
    /// appropriate fragment. Expects that <paramref name="location"/>
    /// refers to this test file or one of its fragments.
    /// </summary>
    public SourceLocation MapToFragment(SourceLocation location)
    {
        if (Fragments.Any(f => f.SourceText == location.SourceText))
            return location;
        
        if (location.SourceText != SourceText)
        {
            throw new ArgumentException(
                $"The given {nameof(location)} does not reference this {nameof(TestFile)} or a fragment thereof.",
                nameof(location));
        }

        var fragment = Fragments.Single(f => f.Location.Contains(location));
        var range = SourceRange.FromLength(location.Start - fragment.Location.Start, location.Length);
        return new SourceLocation(fragment.SourceText, range);
    }

    /// <summary>
    /// Checks, whether this test file can be used for test execution.
    /// </summary>
    public bool Validate(out string errorMessage)
    {
        if (Kind is TestKind.Error)
        {
            errorMessage = "Missing test directive.";
            return false;
        }

        if (Annotations.Count(a => a.Kind is AnnotationKind.CheckDirective or AnnotationKind.RunDirective) > 1)
        {
            errorMessage = "Multiple test directives.";
            return false;
        }

        if (Kind is TestKind.Run && !Annotations.Any(a => a.Kind is AnnotationKind.ExpectOutput))
        {
            errorMessage = "Missing expected output.";
            return false;
        }

        var invalidAnnotations = Annotations.Where(a =>
                a.Kind is AnnotationKind.ExpectError or AnnotationKind.ExpectLint
                && a.Text.Text.Trim() is "")
            .Select(a => a.Prefix.StartLine).ToList();
        if (invalidAnnotations.Count > 0)
        {
            errorMessage = $"Error or Lint annotation with missing ID at ll. {string.Join(", ", invalidAnnotations)}";
            return false;
        }

        errorMessage = "";
        return true;
    }

    
    
    private static ImmutableArray<Annotation> ParseAnnotations(SourceText sourceText)
    {
        // Just run the lexer, since it already knows best where to find
        // the correct comments in sourceText text.

        var annotations = ImmutableArray.CreateBuilder<Annotation>();
        var annotationLocations = Lexer.Lex(sourceText, new DiagnosticBag())
            .Where(t => t.Kind is TokenKind.Comment)
            .Select(t => sourceText.GetLocation(t.FullRange))
            .Where(l => l.Text.StartsWith("//~") || l.Text.StartsWith("//=") || l.Text.StartsWith("//@"));

        foreach (var location in annotationLocations)
        {
            if (ParseSingleAnnotation(location) is { } annotation)
                annotations.Add(annotation);
        }

        return annotations.DrainToImmutable();
    }

    private static Annotation? ParseSingleAnnotation(SourceLocation location)
    {
        var isSingleLine = location.SourceText.Lines[location.StartLine].Start == location.Start
                           && location.StartLine == location.EndLine;
        var text = location.Text;

        if (isSingleLine && text.TrimEnd() is "//@run")
        {
            return new Annotation(AnnotationKind.RunDirective,
                Prefix: location,
                Text: SourceLocation.EmptyAfter(location));
        }

        if (isSingleLine && text.TrimEnd() is "//@check")
        {
            return new Annotation(AnnotationKind.CheckDirective,
                Prefix: location,
                Text: SourceLocation.EmptyAfter(location));
        }

        if (text.StartsWith("//@"))
            return null;

        return TryMakeWithPrefix("//=", AnnotationKind.ExpectOutput)
               ?? TryMakeWithPrefix("//~error", AnnotationKind.ExpectError)
               ?? TryMakeWithPrefix("//~lint", AnnotationKind.ExpectLint);

        Annotation? TryMakeWithPrefix(string prefix, AnnotationKind kind)
        {
            if (!location.Text.StartsWith(prefix))
                return null;
            var prefixLocation = location.FromLengthInside(0, prefix.Length);
            var textLocation = location.FromBoundsInside(prefix.Length, location.Length);
            return new Annotation(kind, prefixLocation, textLocation);
        }
    }

    private static ImmutableArray<Fragment> ParseFragments(SourceText sourceText)
    {
        // Fragments are separated by "//---" that must start the line (modulo whitespace).
        // A fragment contains everything starting with the delimiter up
        // to, but excluding, the next delimiter.
        
        if (sourceText.Lines.Length == 0)
            return [new Fragment(sourceText.GetLocation(SourceRange.FromLength(0, sourceText.Length)))];
        
        var splitLines = Enumerable.Range(0, sourceText.Lines.Length)
            .Where(i => sourceText.GetText(sourceText.Lines[i].Range).TrimStart().StartsWith("//---"))
            .ToList();
        
        // The very first line must start a fragment.
        if (splitLines.Count == 0 || splitLines[0] != 0)
            splitLines.Insert(0, 0);
        
        var fragments = ImmutableArray.CreateBuilder<Fragment>();
        
        for (var i = 0; i < splitLines.Count; i++)
        {
            // Fragment ends on the line before the next delimiter
            // or on the last line.
            var firstLine = splitLines[i];
            var lastLine = i + 1 < splitLines.Count
                ? splitLines[i + 1] - 1
                : sourceText.Lines.Length - 1;
            
            var range = SourceRange.FromTo(sourceText.Lines[firstLine].RangeWithLineBreak, sourceText.Lines[lastLine].RangeWithLineBreak);
            var location = sourceText.GetLocation(range);
            
            fragments.Add(new Fragment(location));
        }
    
        return fragments.DrainToImmutable();
    }
}