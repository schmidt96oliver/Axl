namespace Axl.Compiler;

/// <summary>
/// Span of text inside <see cref="SourceFile"/>.
/// Never refers to <see cref="SourceFileView"/>.
/// 
/// Indices are UTF-16 code unit indices as indexed by <see cref="string"/>.
/// </summary>
/// <remark>
/// A <see cref="SourceSpan"/> does not keep a reference to the <see cref="SourceFile"/>
/// it refers to. For that, use <see cref="SourceLocation"/>.
/// </remark>
public readonly record struct SourceSpan
{
    public int First { get; }
    public int Length { get; }


    /// <summary>
    /// Exclusive end index.
    /// </summary>
    public int End => First + Length;

    public bool IsEmpty => Length == 0;


    private SourceSpan(int first, int length)
    {
        First = first;
        Length = length;
    }

    internal static SourceSpan InsideSourceFile(int first, int length)
    {
        Guard.InRange(first >= 0);
        Guard.InRange(length >= 0);

        return new SourceSpan(first, length);
    }

    public static SourceSpan FromTo(SourceSpan first, SourceSpan last)
    {
        Guard.InRange(first.First <= last.End);
        return new SourceSpan(first.First, length: last.End - first.First);
    }

    public static SourceSpan EmptyBefore(SourceSpan span)
        => new(span.First, length: 0);

    public static SourceSpan EmptyAfter(SourceSpan span)
        => new(span.End, length: 0);

    public static SourceSpan Between(SourceSpan left, SourceSpan right)
    {
        Guard.InRange(left.End <= right.First);
        return new SourceSpan(left.End, length: right.First - left.End);
    }
    
    
    public bool Contains(int index)
        => index >= First && index < End;

    public bool Contains(SourceSpan span)
        => span.First >= First && span.End <= End;

    /// <summary>
    /// Whether the given spans are sequential without overlaps or gaps and span
    /// the entire <see cref="SourceSpan"/>.
    /// If <paramref name="spans"/> is empty, returns true iff this span is empty.
    /// </summary>
    public bool IsPartitionedBy(params IEnumerable<SourceSpan> spans)
    {
        var position = First;
        foreach (var span in spans)
        {
            if (span.First != position)
                return false;
            position = span.End;
        }

        return position == End;
    }
    
    public override string ToString()
        => $"[{First}, {End})";


    

}