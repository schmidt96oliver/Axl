using System.Diagnostics;

namespace Axl.Compiler;

/// <summary>
/// Represents a span of text inside a specific text file.
/// Indices are UTF-16 code point indices as indexed by <see cref="string"/>.
/// </summary>
public readonly record struct SourceSpan
{
    public FileId FileId { get; }
    
    public int First { get; }
    public int Length { get; }


    /// <summary>
    /// Exclusive end index.
    /// </summary>
    public int End => First + Length;

    public bool IsEmpty => Length == 0;


    private SourceSpan(FileId fileId, int first, int length)
    {
        Debug.Assert(first >= 0);
        Debug.Assert(length >= 0);

        FileId = fileId;
        First = first;
        Length = length;
    }


    public static SourceSpan FromTo(FileId fileId, int first, int end)
    {
        Guard.InRange(first, first >= 0);
        Guard.InRange(end, end >= first);
        
        return new SourceSpan(fileId, first, end - first);
    }

    public static SourceSpan FromTo(SourceSpan first, SourceSpan last)
    {
        if (first.FileId != last.FileId)
            throw new ArgumentException("Spans must have same FileId.", nameof(first));
        
        Guard.InRange(first.First, first.First <= last.End);
        return FromTo(first.FileId, first.First, last.End);
    }

    public static SourceSpan FromLength(FileId fileId, int first, int length)
    {
        Guard.InRange(first, first >= 0);
        Guard.InRange(length, length >= 0);
        
        return new SourceSpan(fileId, first, length);
    }
    
}