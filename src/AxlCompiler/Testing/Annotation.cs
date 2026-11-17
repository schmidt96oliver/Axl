using AxlCompiler.Text;

namespace AxlCompiler.Testing;

public enum AnnotationKind
{
    CheckDirective,
    CompileDirective,
    RunDirective,
    IgnoreGlobalDiagnosticsDirective,
    
    ExpectOutput,
    ExpectError,
    ExpectLint,
    ExpectPanic,
    
    SetFuel,
}

public sealed record Annotation(AnnotationKind Kind, SourceLocation Prefix, SourceLocation Text);