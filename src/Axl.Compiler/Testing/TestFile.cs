using System.Collections.Immutable;
using System.Diagnostics;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public sealed class TestFile
{
    public SourceFile Source { get; }
    
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
                var tree = Parser.Parse(fragment.Source);
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


    private TestFile(SourceFile source, Directive? directive, 
        ImmutableArray<Fragment> fragments, ImmutableArray<Diagnostic> diagnostics)
    {
        Source = source;
        Directive = directive;
        Fragments = fragments;
        Diagnostics = diagnostics;
    }
    

    public static TestFile From(SourceFile source)
    {
        var diagnostics = new DiagnosticBag();
        var parser = new TaxlParser(source, diagnostics);
        
        var fragments = parser.ParseFragments();
        Debug.Assert(fragments.Length > 0, "There must be at least one fragment.");

        var directive = parser.ParseDirective();
        return new TestFile(source, directive, fragments, diagnostics.Drain());
    }

    
    
    public Fragment GetFragmentAt(SourceLocation location)
        => Fragments.FirstOrDefault(fragment => fragment.Source.File == location.File &&
                                                fragment.Source.Span.Contains(location.Span))
           ?? throw new ArgumentException($"{nameof(location)} is not contained in this {nameof(TestFile)}.");
}