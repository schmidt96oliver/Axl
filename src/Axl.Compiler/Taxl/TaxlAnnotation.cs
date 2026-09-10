using Axl.Compiler.Text;

namespace Axl.Compiler.Taxl;

public enum DiagnosticKind
{
    Error,
    Lint
}

public abstract record TaxlAnnotation(SourceSpan AnnotationSpan, SourceSpan ArgumentSpan)
{
    public sealed record Diagnostic(
        DiagnosticKind Kind,
        string Id,
        int LineNumber,
        SourceSpan AnnotationSpan,
        SourceSpan ArgumentSpan)
        : TaxlAnnotation(AnnotationSpan, ArgumentSpan);

    public sealed record Type(
        SourceSpan ExprSpan,
        string TypeName,
        SourceSpan AnnotationSpan,
        SourceSpan ArgumentSpan)
        : TaxlAnnotation(AnnotationSpan, ArgumentSpan);

    public sealed record Invalid(string ErrorMessage, SourceSpan AnnotationSpan)
        : TaxlAnnotation(AnnotationSpan, ArgumentSpan: SourceSpan.EmptyAfter(AnnotationSpan));
}