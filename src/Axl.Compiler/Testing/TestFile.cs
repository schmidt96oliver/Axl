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
    
    public ImmutableArray<Fragment> Fragments { get; }
    
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
                var fragment = Fragments.First(fragment => !fragment.IsOutput);
                var tree = Parser.Parse(fragment.Text);
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


    private TestFile(SourceText sourceText, Directive? directive, 
        ImmutableArray<Fragment> fragments, ImmutableArray<Diagnostic> diagnostics)
    {
        SourceText = sourceText;
        Directive = directive;
        Fragments = fragments;
        Diagnostics = diagnostics;
    }
    

    public static TestFile From(SourceText sourceText)
    {
        var diagnostics = new DiagnosticBag();
        var parser = new TaxlParser(sourceText, diagnostics);
        
        var fragments = parser.ParseFragments();
        Debug.Assert(fragments.Length > 0, "There must be at least one fragment.");

        var directive = parser.ParseDirective();
        return new TestFile(sourceText, directive, fragments, diagnostics.Drain());
    }

    
    
    public Fragment GetFragmentAt(SourceLocation location)
        => Fragments.FirstOrDefault(fragment => fragment.Source.File == location.SourceText &&
                                                fragment.Source.Range.Contains(location.Range))
           ?? throw new ArgumentException($"{nameof(location)} is not contained in this {nameof(TestFile)}.");
}