using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public enum AnnotationKind
{
    CheckDirective,
    RunDirective,
    ExpectOutput,
    ExpectError,
    ExpectLint,
}

public sealed record Annotation(AnnotationKind Kind, SourceLocation Prefix, SourceLocation Text);