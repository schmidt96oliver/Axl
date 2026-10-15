using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public sealed class TestFile
{
    public SourceText SourceText { get; }
    
    public Directive Directive { get; }
    public Expectation? Expectation { get; }
    public ImmutableArray<Fragment> Fragments { get; }

    public ImmutableArray<DiagnosticAnnotation> Annotations { get; }

    /// <summary>
    /// Diagnostics emitted during parsing of this test file.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }


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
        Directive directive,
        Expectation? expectation,
        ImmutableArray<Fragment> fragments,
        ImmutableArray<DiagnosticAnnotation> annotations,
        ImmutableArray<Diagnostic> diagnostics)
    {
        SourceText = sourceText;
        Directive = directive;
        Expectation = expectation;
        Fragments = fragments;
        Annotations = annotations;
        Diagnostics = diagnostics;
    }


    public static TestFile From(SourceText sourceText)
    {
        var diagnostics = new DiagnosticBag();
        var parser = new TaxlParser(sourceText, diagnostics);
        
        var directive = parser.ParseDirective();
        var expectation = parser.ParseExpectation();
        var annotations = parser.ParseAnnotations();
        var fragments = parser.ParseFragments();
        
        return new TestFile(sourceText, directive, expectation, fragments, annotations, diagnostics.Drain());
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

        var fragment = Fragments.SingleOrDefault(frag => frag.LocalLocation.Contains(location))
            ?? throw new ArgumentException($"The given {nameof(location)} does not reference this {nameof(TestFile)} or a fragment thereof.", nameof(location));
        var range = SourceRange.FromLength(location.Start + fragment.LocationInTestFile.Start, location.Length);
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

        var fragment = Fragments.Single(f => f.LocationInTestFile.Contains(location));
        var range = SourceRange.FromLength(location.Start - fragment.LocationInTestFile.Start, location.Length);
        return new SourceLocation(fragment.SourceText, range);
    }
}