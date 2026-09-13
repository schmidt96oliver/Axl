using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public sealed class TestFile
{
    public SourceText SourceText { get; }
    
    public Directive? Directive { get; }
    
    public ImmutableArray<Annotation> Annotations { get; }

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
        Directive? directive,
        ImmutableArray<Annotation> annotations,
        ImmutableArray<Diagnostic> diagnostics)
    {
        SourceText = sourceText;
        Directive = directive;
        Annotations = annotations;
        Diagnostics = diagnostics;
    }


    public static TestFile From(SourceText sourceText)
    {
        var diagnostics = new DiagnosticBag();
        var parser = new TaxlParser(sourceText, diagnostics);
        
        var directive = parser.ParseDirective();
        var annotations = parser.ParseAnnotations();
        
        return new TestFile(sourceText, directive, annotations, diagnostics.Drain());
    }
}