namespace Axl.Compiler.Text;

public readonly record struct Line(int First, int Length, int LengthWithLineBreak)
{
    public SourceRange Range => SourceRange.FromLength(First, Length);
    public SourceRange RangeWithLineBreak => SourceRange.FromLength(First, LengthWithLineBreak);
}