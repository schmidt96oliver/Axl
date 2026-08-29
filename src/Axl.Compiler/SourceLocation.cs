namespace Axl.Compiler;

/// <summary>
/// Span of text inside <see cref="SourceFile"/> which
/// carries the reference to its <see cref="SourceFile"/>.
/// </summary>
public readonly record struct SourceLocation(SourceFile File, SourceSpan Span)
{
    public LinePosition StartLinePosition => File.GetLinePositionOrEof(Span.First);

    public LinePosition EndLinePosition => File.GetLinePositionOrEof(Span.End);
    
    public ReadOnlySpan<char> GetText()
        => File.GetText(Span);
    
}