using AxlCompiler.Text;

namespace AxlCompiler.Testing;

public enum AnnotationKind
{
    CheckDirective,
    RunDirective,
    IgnoreGlobalDiagnosticsDirective,
    
    ExpectOutput,
    ExpectError,
    ExpectLint,
    ExpectPanic,
}

public sealed record Annotation(AnnotationKind Kind, SourceLocation Prefix, SourceLocation Text);