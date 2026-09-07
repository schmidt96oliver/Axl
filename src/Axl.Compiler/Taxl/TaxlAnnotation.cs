namespace Axl.Compiler.Taxl;

public enum DiagnosticKind
{
    Error,
    Lint
}

public abstract record TaxlAnnotation(SourceSpan AnnotationSpan)
{
    public sealed record Diagnostic(DiagnosticKind Kind, string Id, int LineNumber, SourceSpan AnnotationSpan)
        : TaxlAnnotation(AnnotationSpan);
    public sealed record Type(SourceSpan ExprSpan, string TypeName, SourceSpan AnnotationSpan)
        : TaxlAnnotation(AnnotationSpan);

    public sealed record Invalid(string ErrorMessage, SourceSpan AnnotationSpan)
        : TaxlAnnotation(AnnotationSpan);
}