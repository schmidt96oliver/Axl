using System.Collections.Immutable;
using System.Diagnostics;

namespace Axl.Compiler.Text;

public class SourceText
{
    public string Text { get; }

    public int Length => Text.Length;

    public ImmutableArray<Line> Lines
    {
        get
        {
            if (field.IsDefault)
                field = ParseLines();
            return field;
        }
    }
    
    
    private SourceText(string text)
    {
        Text = text;
    }

    public static SourceText From(string text)
        => new(text);

    
    public ReadOnlySpan<char> GetText(SourceRange range)
    {
        Guard.MustBe(range.First >= 0);
        Guard.MustBe(range.End <= Length);

        return Text.AsSpan(range.First, range.Length);
    }

    public int GetLineIndex(int position)
    {
        // 012\n
        // 456\n
        // 789\n
        
        // 012

        var lower = 0;
        var upper = Lines.Length - 1;
        
        Debug.Assert(upper >= 0, "There is always at least one line.");

        while (lower <= upper)
        {
            var index = lower + (upper - lower) / 2;
            var lineStart = Lines[index].First;

            if (position == lineStart) 
                return index;

            if (position > lineStart)
                lower = index + 1;
            else
                upper = index - 1;
        }

        return lower - 1;
    }
    
    private ImmutableArray<Line> ParseLines()
    {
        var lines = ImmutableArray.CreateBuilder<Line>();

        var lineStart = 0;
        for (var i = 0; i < Length; )
        {
            var lineBreakLength = GetLineBreakLength(i);

            if (lineBreakLength > 0)
            {
                var length = i - lineStart;
                var line = new Line(lineStart, length, length + lineBreakLength);
                lines.Add(line);
                
                i += lineBreakLength;
                lineStart = i;
            }
            else
            {
                i++;
            }
        }

        if (lineStart <= Length)
        {
            var lastLineLength = Length - lineStart;
            var lastLine = new Line(lineStart, lastLineLength, lastLineLength);
            lines.Add(lastLine);
        }

        return lines.DrainToImmutable();

        int GetLineBreakLength(int position)
        {
            var next = position + 1 < Length ? Text[position + 1] : '\0';
            return (Text[position], next) switch
            {
                ('\r', '\n') => 2,
                ('\n', _) or ('\r', _) => 1,
                _ => 0
            };
        }
    }
}