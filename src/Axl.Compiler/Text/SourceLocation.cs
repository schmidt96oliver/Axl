using System.Diagnostics;

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
    public int StartColumn => Start - SourceText.Lines[StartLine].Start;

    public int EndLine => SourceText.GetLineIndex(End);
    public int EndColumn => End - SourceText.Lines[StartLine].Start;


    public ReadOnlySpan<char> Text => SourceText.GetText(Range);
    public SourceText SourceText { get; }
    public SourceRange Range { get; }
    
    
    /// <summary>
    /// Only constructed through <see cref="SourceText.GetLocation"/>.
    /// </summary>
    public SourceLocation(SourceText sourceText, SourceRange range)
    {
        SourceText = sourceText;
        Range = range;
    }

    public bool Contains(SourceLocation location)
        => SourceText == location.SourceText && Range.Contains(location.Range);

    public SourceLocation FromLengthInside(int start, int length)
    {
        Guard.MustBe(start >= 0);
        Guard.MustBe(start + length <= Length);

        var range = SourceRange.FromLength(start + Start, length);
        Debug.Assert(Range.Contains(range));
        return new SourceLocation(SourceText, range);
    }

    public SourceLocation FromBoundsInside(int start, int end)
    {
        Guard.MustBe(start >= 0);
        Guard.MustBe(start <= end);
        Guard.MustBe(end <= Length);

        var range = SourceRange.FromBounds(start + Start, end + Start);
        Debug.Assert(Range.Contains(range));
        return new SourceLocation(SourceText, range);
    }

    public static SourceLocation FromBounds(SourceText sourceText, int start, int end)
    {
        Guard.MustBe(start >= 0);
        Guard.MustBe(end <= sourceText.Length);

        var range = SourceRange.FromBounds(start, end);
        return new SourceLocation(sourceText, range);
    }
}