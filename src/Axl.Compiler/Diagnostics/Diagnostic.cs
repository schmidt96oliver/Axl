namespace Axl.Compiler.Diagnostics;

public abstract partial record Diagnostic
{
    public virtual string Id => GetType().Name;
    
    public abstract SourceLocation Location { get; }
    public abstract DiagnosticSeverity DefaultSeverity { get; }
    public abstract string Message { get; }


    public virtual string? Hint => null;
    public virtual IReadOnlyList<LabeledSourceLocation> Related => [];


    // Seal the class. Diagnostics are only defined in here.
    private protected Diagnostic()
    { }

    // I need the error-lint distinction, so that the C# type system ensures, that I
    // actually create an error, when I want an error. It prevents silent bugs
    // where severity is not error, but I expected it to be. That's important,
    // because following compilation passes might guard on errors :).
    
    public abstract record Error : Diagnostic
    {
        public override DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Error;
    }

    public abstract record Lint : Diagnostic;
}