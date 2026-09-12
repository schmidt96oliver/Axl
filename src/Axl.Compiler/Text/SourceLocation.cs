namespace Axl.Compiler.Text;

/// <summary>
/// Range of text inside <see cref="Text.SourceText"/> including
/// a reference to it.
/// </summary>
public readonly record struct SourceLocation(SourceText SourceText, SourceRange Range)
{
    public int First => Range.First;

    public int End => Range.End;
    
    public int Length => Range.Length;
    
    public int FirstLine => SourceText.GetLineIndex(First);
    public int FirstColumn => First - SourceText.Lines[FirstLine].First;

    public int EndLine => SourceText.GetLineIndex(End);
    public int EndColumn => End - SourceText.Lines[FirstLine].First;


    public ReadOnlySpan<char> Text => SourceText.GetText(Range);
}