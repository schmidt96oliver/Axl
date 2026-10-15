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
            if (field is null)
            {
                var tree = Parser.Parse(SourceText);
                field = Compilation.From(tree);
            }
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
}