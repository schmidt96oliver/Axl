namespace Axl.Compiler;

/// <summary>
/// A view into one part of a <see cref="SourceFile"/>.
/// This is all the compiler will ever see, so
/// that only parts of a file can be passed into the pipeline.
/// </summary>
public readonly record struct SourceFileView(SourceFile File, SourceSpan Span)
{
    public ReadOnlySpan<char> TextSpan => File.GetText(Span);
    
    
    public static SourceFileView Whole(SourceFile file)
        => new(file, SourceSpan.InsideSourceFile(0, length: file.Text.Length));
    
    public static SourceFileView FromFile(string path)
        => Whole(SourceFile.FromFile(path));

    public static SourceFileView FromText(string text)
        => Whole(SourceFile.FromText(text));

    /// <summary>
    /// Converts start/end indices inside this view to <see cref="SourceSpan"/> that
    /// references the containing file.
    /// </summary>
    /// <param name="end">Exclusive end index inside source file.</param>
    public SourceSpan SpanFromTo(int start, int end)
    {
        Guard.InRange(start >= 0);
        Guard.InRange(end <= Span.Length);
        return SourceSpan.InsideSourceFile(start + Span.First, length: end - start);
    }
    
    /// <summary>
    /// Converts start index and length inside this view to <see cref="SourceSpan"/> that
    /// references the containing file.
    /// </summary>
    public SourceSpan SpanFromLength(int start, int length)
    {
        Guard.InRange(start >= 0);
        Guard.InRange(length >= 0);
        Guard.InRange(length <= Span.Length);
        return SourceSpan.InsideSourceFile(start + Span.First, length);
    }

    public SourceLocation GetLocation(SourceSpan span)
        => new(File, span);
    
    public SourceLocation LocationFromTo(int start, int end)
        => GetLocation(SpanFromTo(start, end));
    
    public SourceLocation LocationFromLength(int start, int length)
        => GetLocation(SpanFromLength(start, length));

    public ReadOnlySpan<char> GetText(SourceSpan span)
    {
        Guard.InRange(Span.Contains(span));
        return File.GetText(span);
    }
}