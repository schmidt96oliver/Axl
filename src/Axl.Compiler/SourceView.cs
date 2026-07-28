namespace Axl.Compiler;

/// <summary>
/// A view into a <see cref="SourceFile"/>.
/// This is all the compiler will ever see, so
/// that only parts of a file can be passed into the pipeline.
/// </summary>
public readonly record struct SourceView(SourceFile File, SourceSpan Span)
{
    public ReadOnlySpan<char> TextSpan => File.GetTextSpan(Span);
    
    
    public static SourceView Whole(SourceFile file)
        => new(file, SourceSpan.FromLength(0, file.Text.Length));
    
    public static SourceView FromFile(string path)
        => Whole(SourceFile.FromFile(path));

    /// <summary>
    /// Converts start/end indices inside this view to <see cref="SourceSpan"/> that
    /// references the containing file.
    /// </summary>
    public SourceSpan GetSpanFromTo(int start, int end)
    {
        Guard.InRange(start, start >= 0);
        Guard.InRange(end, end <= Span.Length);
        return SourceSpan.FromTo(start + Span.First, end + Span.First);
    }
    
    /// <summary>
    /// Converts start index and length inside this view to <see cref="SourceSpan"/> that
    /// references the containing file.
    /// </summary>
    public SourceSpan GetSpanFromLength(int start, int length)
    {
        Guard.InRange(start, start >= 0);
        Guard.InRange(length, length >= 0);
        Guard.InRange(length, length <= Span.Length);
        return SourceSpan.FromLength(start + Span.First, length);
    }

    public SourceLocation GetLocation(SourceSpan span)
        => File.GetLocation(span);
    
    public SourceLocation GetLocationFromTo(int start, int end)
        => GetLocation(GetSpanFromTo(start, end));
    
    public SourceLocation GetLocationFromLength(int start, int length)
        => GetLocation(GetSpanFromTo(start, length));

    public string GetText(SourceSpan span)
    {
        Guard.InRange(span, Span.Contains(span));
        return File.GetText(span);
    }
}