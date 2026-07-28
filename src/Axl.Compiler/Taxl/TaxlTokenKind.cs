namespace Axl.Compiler.Taxl;

public enum TaxlTokenKind
{
    Directive,
    Identifier,
    String,
    AxlText,

    Comment,
    Whitespace,
    Error,
    Newline
}