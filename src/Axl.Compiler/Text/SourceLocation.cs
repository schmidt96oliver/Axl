namespace Axl.Compiler.Text;

/// <summary>
/// Range of text inside <see cref="Text.SourceText"/> including
/// a reference to it.
/// </summary>
public readonly record struct SourceLocation
{
    public int Start => Range.Start;

    public int End => Range.End;
    
    public int Length => Range.Length;
    
    public int StartLine => SourceText.GetLineIndex(Start);
    public int StartColumn => Start - SourceText.Lines[StartLine].First;

    public int EndLine => SourceText.GetLineIndex(End);
    public int EndColumn => End - SourceText.Lines[StartLine].First;


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
    
    public static SourceLocation FromBounds(SourceText sourceText, int start, int end)
    {
        var range = SourceRange.FromBounds(start, end);
        Guard.MustBe(sourceText.Contains(range));
        return new SourceLocation(sourceText, range);
    }

    public static SourceLocation FromLength(SourceText sourceText, int start, int length)
    {
        var range = SourceRange.FromLength(start, length);
        Guard.MustBe(sourceText.Contains(range));
        return new SourceLocation(sourceText, range);
    }
}