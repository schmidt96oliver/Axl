namespace Axl.Compiler.Text;

/// <summary>
/// Range of text inside <see cref="Text.SourceText"/> including
/// a reference to it.
/// </summary>
public readonly record struct SourceLocation
{
    public int First => Range.First;

    public int End => Range.End;
    
    public int Length => Range.Length;
    
    public int FirstLine => SourceText.GetLineIndex(First);
    public int FirstColumn => First - SourceText.Lines[FirstLine].First;

    public int EndLine => SourceText.GetLineIndex(End);
    public int EndColumn => End - SourceText.Lines[FirstLine].First;


    public ReadOnlySpan<char> Text => SourceText.GetText(Range);
    public SourceText SourceText { get; }
    public SourceRange Range { get; }
    
    
    private SourceLocation(SourceText sourceText, SourceRange range)
    {
        SourceText = sourceText;
        Range = range;
    }

    public static SourceLocation From(SourceText sourceText, SourceRange range)
    {
        Guard.MustBe(sourceText.Contains(range));
        return new SourceLocation(sourceText, range);
    }
    
    public static SourceLocation FromBounds(SourceText sourceText, int first, int end)
    {
        var range = SourceRange.FromBounds(first, end);
        Guard.MustBe(sourceText.Contains(range));
        return new SourceLocation(sourceText, range);
    }

    public static SourceLocation FromLength(SourceText sourceText, int first, int length)
    {
        var range = SourceRange.FromLength(first, length);
        Guard.MustBe(sourceText.Contains(range));
        return new SourceLocation(sourceText, range);
    }

    public void Deconstruct(out SourceText SourceText, out SourceRange Range)
    {
        SourceText = this.SourceText;
        Range = this.Range;
    }
}