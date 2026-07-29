using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Taxl;

public record TaxlToken
{
    public sealed record AxlTextToken(SourceSpan Span, string Text, ImmutableArray<TaxlToken> InTextTokens) 
        : TaxlToken(Span, TaxlTokenKind.AxlText, Text);
    
    
    public SourceSpan Span { get; }
    public TaxlTokenKind Kind { get; }
    public string Text { get; }
    
    private TaxlToken(SourceSpan span, TaxlTokenKind kind, string text)
    {
        Span = span;
        Kind = kind;
        Text = text;
    }


    public static TaxlToken Simple(SourceSpan span, TaxlTokenKind kind, string text)
    {
        if (kind is TaxlTokenKind.Error or TaxlTokenKind.AxlText)
        {
            throw new ArgumentException("Error and AxlText must be created through special constructors.",
                nameof(kind));
        }

        return new TaxlToken(span, kind, text);
    }

    public static TaxlToken Error(ErrorGuaranteed proof, SourceSpan span, string text)
        => new TaxlToken(span, TaxlTokenKind.Error, text);
    
    public static AxlTextToken AxlText(SourceSpan span, string text, ImmutableArray<TaxlToken> inTextTokens)
        => new AxlTextToken(span, text, inTextTokens);
}