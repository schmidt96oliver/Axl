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
    
    
    /// <summary>
    /// Only constructed through <see cref="SourceText.GetLocation"/>.
    /// </summary>
    internal SourceLocation(SourceText sourceText, SourceRange range)
    {
        SourceText = sourceText;
        Range = range;
    }
}