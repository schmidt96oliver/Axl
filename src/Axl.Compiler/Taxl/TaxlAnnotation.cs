namespace Axl.Compiler.Taxl;

public enum DiagnosticKind
{
    Error,
    Lint
}

public abstract record TaxlAnnotation(SourceSpan AnnotationSpan, SourceSpan PrefixAndLocatorSpan)
{
    public sealed record Diagnostic(
        DiagnosticKind Kind,
        string Id,
        int LineNumber,
        SourceSpan AnnotationSpan,
        SourceSpan PrefixAndLocatorSpan)
        : TaxlAnnotation(AnnotationSpan, PrefixAndLocatorSpan);

    public sealed record Type(
        SourceSpan ExprSpan,
        string TypeName,
        SourceSpan AnnotationSpan,
        SourceSpan PrefixAndLocatorSpan)
        : TaxlAnnotation(AnnotationSpan, PrefixAndLocatorSpan);

    public sealed record Invalid(string ErrorMessage, SourceSpan AnnotationSpan)
        : TaxlAnnotation(AnnotationSpan, PrefixAndLocatorSpan: AnnotationSpan);
}