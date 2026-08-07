using System.Collections.Immutable;

namespace Axl.Compiler.Diagnostics;

public abstract partial record Diagnostic
{
    public virtual string Id => GetType().Name;

    /// <summary>
    /// Every location that is at fault. Never empty. Editors underline all of
    /// them, so only put code in here that is genuinely wrong - code that
    /// merely explains the diagnostic belongs in <see cref="Related"/>.
    /// </summary>
    public abstract ImmutableArray<SourceLocation> Locations { get; }
    
    /// <summary>
    /// The label to render references to other <see cref="Locations"/>
    /// with. For multiple <see cref="Locations"/>, the Lsp will emit one diagnostic
    /// per location and reference the others with this label.
    /// Useless, if there is only one location.
    /// </summary>
    public virtual string LocationLabel => "Also at fault.";
    
    public abstract DiagnosticSeverity DefaultSeverity { get; }
    public abstract string Message { get; }


    public virtual string? Hint => null;

    /// <summary>
    /// Locations that give context for the diagnostic without being at fault
    /// themselves, together with the label to render at them.
    /// </summary>
    public virtual ImmutableArray<LabeledSourceLocation> Related => [];


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