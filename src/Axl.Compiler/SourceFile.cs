using System.Collections.Immutable;
using System.Diagnostics;

namespace Axl.Compiler;

public readonly record struct LineInfo(int LineNumber, SourceSpan Span)
{
    public int Length => Span.Length;
}

public sealed class SourceFile
{
    public string Path { get; }

    public string Text { get; }

    public ImmutableArray<LineInfo> Lines
    {
        get
        {
            if (field.IsDefault)
                field = GetLines();
            return field;
        }
    }


    private SourceFile(string path, string text)
    {
        Path = path;
        Text = text;
    }

    public static SourceFile FromFile(string path)
    {
        var text = File.ReadAllText(path);
        return new SourceFile(path, text);
    }

    public static SourceFile FromText(string path, string text)
    {
        return new SourceFile(path, text);
    }
    

    public LineInfo GetLineAt(int index)
    {
        Guard.InRange(index, index >= 0);
        Guard.InRange(index, index < Text.Length);

        // Implements a binary search
        // There must be at least one line, since the Guards
        // above will throw for every index, if Text is empty.
        Debug.Assert(Lines.Length >= 1);
        
        var searchStart = 0;
        var searchEnd = Lines.Length;   // exclusive

        // Guard against infinite loop with fuel to fail loudly.
        // This algorithm visits every line at most once, so it is
        // guaranteed to finish in Lines.Length iterations.
        
        // With each iteration, [searchStart, searchEnd) becomes
        // strictly smaller or a value is returned. So this
        // loop is guaranteed to return IF lines were calculated correctly.
        var fuel = Lines.Length;
        while (fuel > 0)
        {
            var pivot = searchStart + (searchEnd - searchStart) / 2;
            Debug.Assert(pivot >= searchStart && pivot < searchEnd);
            
            var line = Lines[pivot];
            if (index < line.Span.First)
            {
                // Index is somewhere before this line.
                // So search the previous half
                searchEnd = pivot;
            }
            else if (index >= line.Span.End)
            {
                // There must be another lines, since the index
                // did not come up yet. Otherwise, line calculation
                // was wrong.
                Debug.Assert(pivot + 1 < Lines.Length);
                
                // Index is somewhere after this line.
                // So search the second half.
                searchStart = pivot + 1;
            }
            else
            {
                Debug.Assert(line.Span.Contains(index));
                return line;
            }

            fuel--;
        }

        throw new UnreachableException("Binary search went into infinite loop.");
    }

    public LinePosition GetLinePosition(int index)
    {
        Guard.InRange(index, index >= 0);
        Guard.InRange(index, index < Text.Length);

        var line = GetLineAt(index);

        Debug.Assert(line.Span.Contains(index));
        return new LinePosition(line.LineNumber, index - line.Span.First);
    }



    public ReadOnlySpan<char> GetSpan(SourceSpan span)
    {
        Guard.InRange(span.First, span.First >= 0);
        Guard.InRange(span.Length, span.Length <= Text.Length);
        return Text.AsSpan(span.First, span.Length);
    }
    
    public string GetText(SourceSpan span)
        => GetSpan(span).ToString();
    

    private ImmutableArray<LineInfo> GetLines()
    {
        var builder = ImmutableArray.CreateBuilder<LineInfo>();

        var lineStart = 0;
        var lineIndex = 0;
        for (var currentChar = 0; currentChar < Text.Length; currentChar++)
        {
            if (Text[currentChar] is '\n')
            {
                // This line must include \n, so it spans one further.
                var span = SourceSpan.FromTo(lineStart, currentChar + 1);
                builder.Add(new LineInfo(lineIndex, span));
                
                lineIndex++;
                lineStart = currentChar + 1;
            }

        }

        if (lineStart < Text.Length)
            builder.Add(new LineInfo(lineIndex, SourceSpan.FromTo(lineStart, Text.Length)));
        
        return builder.ToImmutable();
    }
}