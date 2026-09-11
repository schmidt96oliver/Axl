using Axl.Compiler.Text;

namespace Axl.Compiler.Testing;

public enum DiagnosticKind
{
    Error,
    Lint
}

public sealed record DiagnosticAnnotation(
    SourceLocation FullLocation,
    SourceLocation PrefixLocation,
    DiagnosticKind Kind,
    string Id)
    : Annotation(FullLocation, PrefixLocation);