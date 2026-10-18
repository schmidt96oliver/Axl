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
    
    
    public SourceLocation(SourceText sourceText, SourceRange range)
    {
        Guard.MustBe(sourceText.Range.Contains(range));
        
        SourceText = sourceText;
        Range = range;
    }

    public static SourceLocation FromBounds(SourceText sourceText, int start, int end)
    {
        Guard.MustBe(start >= 0);
        Guard.MustBe(end <= sourceText.Length);

        var range = SourceRange.FromBounds(start, end);
        return new SourceLocation(sourceText, range);
    }
    public static SourceLocation FromLength(SourceText sourceText, int start, int length)
    {
        Guard.MustBe(start >= 0);
        Guard.MustBe(start + length <= sourceText.Length);

        var range = SourceRange.FromLength(start, length);
        return new SourceLocation(sourceText, range);
    }

    public static SourceLocation EmptyAfter(SourceLocation location)
        => new(location.SourceText, SourceRange.EmptyAfter(location.Range));
    
    public static SourceLocation EmptyBefore(SourceLocation location)
        => new(location.SourceText, SourceRange.EmptyBefore(location.Range));

    public static SourceLocation FromTo(SourceLocation from, SourceLocation to)
    {
        Guard.MustBe(from.SourceText == to.SourceText);
        return new SourceLocation(from.SourceText, SourceRange.FromTo(from.Range, to.Range));
    }
    

    public bool Contains(SourceLocation location)
        => SourceText == location.SourceText && Range.Contains(location.Range);

    public SourceLocation this[Range range] => new (SourceText, Range[range]);
}