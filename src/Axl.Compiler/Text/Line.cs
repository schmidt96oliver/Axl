namespace Axl.Compiler.Text;

public readonly record struct Line(int First, int Length, int LengthWithLineBreak)
{
    public SourceSpan Span => SourceSpan.InsideSourceFile(First, Length);
    public SourceSpan SpanWithLineBreak => SourceSpan.InsideSourceFile(First, LengthWithLineBreak);
}