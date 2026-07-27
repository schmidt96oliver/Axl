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
    

    /// <summary>
    /// Binary search into <see cref="Lines"/>.
    /// </summary>
    public LineInfo GetLineAt(int index)
    {
        Guard.InRange(index, index >= 0);
        Guard.InRange(index, index < Text.Length);

        var searchStart = 0;
        var searchEnd = Lines.Length;

        while (searchStart < searchEnd)
        {
            var pivot = searchStart + (searchEnd - searchStart) / 2;
            Debug.Assert(pivot < Lines.Length);
            
            var line = Lines[pivot];
            if (index < line.Span.First)
            {
                // Index is somewhere before this line.
                // So search the previous half
                searchStart = searchStart;
                searchEnd = pivot;
            }
            else if (index >= line.Span.End)
            {
                // Index is somewhere after this line.
                // So search the second half.
                searchStart = pivot + 1;
                searchEnd = searchEnd;
            }
            else
            {
                Debug.Assert(line.Span.Contains(index));
                return line;
            }
        }

        throw new UnreachableException("Binary search must finish, since index is guarded.");
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