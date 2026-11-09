using AxlCompiler.Text;

namespace AxlCompiler.Testing;

public enum AnnotationKind
{
    CheckDirective,
    RunDirective,
    ExpectOutput,
    ExpectError,
    ExpectLint,
}

public sealed record Annotation(AnnotationKind Kind, SourceLocation Prefix, SourceLocation Text);