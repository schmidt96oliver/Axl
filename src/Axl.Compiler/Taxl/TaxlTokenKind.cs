namespace Axl.Compiler.Taxl;

public enum TaxlTokenKind
{
    Directive,
    InTextDirective,
    Identifier,
    String,
    AxlText,

    Comment,
    Whitespace,
    Error,
    Newline
}