namespace Axl.Compiler.Text;

/// <summary>
/// Range of text inside <see cref="SourceText"/>.
/// Indices are UTF-16 code unit indices as indexed by <see cref="string"/>.
/// </summary>
/// <remark>
/// A <see cref="SourceRange"/> does not keep a reference to the <see cref="SourceText"/>
/// it refers to. For that, use <see cref="SourceLocation"/>.
/// </remark>
public readonly record struct SourceRange
{
    public int First { get; }
    public int Length { get; }

    /// <summary>
    /// Exclusive
    /// </summary>
    public int End => First + Length;

    public bool IsEmpty => Length == 0;


    private SourceRange(int first, int length)
    {
        First = first;
        Length = length;
    }

    
    public static SourceRange FromBounds(int first, int end)
    {
        Guard.MustBe(end >= first);
        return new SourceRange(first, end - first);
    }

    public static SourceRange FromLength(int first, int length)
    {
        Guard.MustBe(length >= 0);
        return new SourceRange(first, length);
    }
    
    public static SourceRange FromTo(SourceRange first, SourceRange last)
    {
        Guard.InRange(first.First <= last.End);
        return new SourceRange(first.First, length: last.End - first.First);
    }

    public static SourceRange EmptyAt(int position)
        => new(position, 0);
    
    public static SourceRange EmptyBefore(SourceRange range)
        => EmptyAt(range.First);

    public static SourceRange EmptyAfter(SourceRange range)
        => EmptyAt(range.End);

    public static SourceRange Between(SourceRange left, SourceRange right)
    {
        Guard.InRange(left.End <= right.First);
        return new SourceRange(left.End, length: right.First - left.End);
    }
    
    
    public bool Contains(int index)
        => index >= First && index < End;

    public bool Contains(SourceRange range)
        => range.First >= First && range.End <= End;

    /// <summary>
    /// Whether the given ranges are sequential without overlaps or gaps and range
    /// the entire <see cref="SourceRange"/>.
    /// If <paramref name="ranges"/> is empty, returns true iff this range is empty.
    /// </summary>
    public bool IsPartitionedBy(params IEnumerable<SourceRange> ranges)
    {
        var position = First;
        foreach (var range in ranges)
        {
            if (range.First != position)
                return false;
            position = range.End;
        }

        return position == End;
    }
    
    public override string ToString()
        => $"[{First}, {End})";
}