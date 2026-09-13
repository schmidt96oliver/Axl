namespace Axl.Compiler.Text;

public readonly record struct Line(int Start, int Length, int LengthWithLineBreak)
{
    public SourceRange Range => SourceRange.FromLength(Start, Length);
    public SourceRange RangeWithLineBreak => SourceRange.FromLength(Start, LengthWithLineBreak);
}