namespace Axl.Compiler;

/// <summary>
/// Span of text inside <see cref="SourceFile"/> which
/// carries the reference to its <see cref="SourceFile"/>.
/// </summary>
public readonly record struct SourceLocation(SourceFile File, SourceSpan Span)
{
    public LinePosition GetFirstLinePosition()
        => File.GetLinePosition(Span.First);
    
    public ReadOnlySpan<char> GetText()
        => File.GetText(Span);

}