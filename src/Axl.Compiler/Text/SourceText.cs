using System.Collections.Immutable;
using System.Diagnostics;

namespace Axl.Compiler.Text;

public class SourceText
{
    public string Text { get; }
    
    /// <summary>
    /// The filename, this text was loaded from.
    /// <c>null</c>, if it was initialized from pure text.
    /// </summary>
    public string? FileName { get; }

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

    public SourceRange Range => SourceRange.FromLength(0, Length);


    private SourceText(string text, string? fileName)
    {
        Text = text;
        FileName = fileName;
    }

    public static SourceText From(string text, string? fileName = null)
        => new(text, fileName);
    
    public static SourceText LoadFile(string path)
    {
        var text = File.ReadAllText(path);
        return new SourceText(text, path);
    }


    public char this[int index]
        => Text[index];

    public ReadOnlySpan<char> this[Range range]
        => Text[range];
    
    
    public ReadOnlySpan<char> GetText(SourceRange range)
    {
        Guard.MustBe(range.Start >= 0);
        Guard.MustBe(range.End <= Length);

        return Text.AsSpan(range.Start, range.Length);
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

    public bool Contains(SourceRange range)
        => range.Start >= 0 && range.End <= Length;


    public override string ToString()
        => Text;
}