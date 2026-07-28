namespace Axl.Compiler.Taxl;

public readonly record struct TaxlToken(SourceSpan Span, TaxlTokenKind Kind, string Text);