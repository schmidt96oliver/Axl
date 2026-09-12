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
    public int Start { get; }
    public int Length { get; }

    /// <summary>
    /// Exclusive
    /// </summary>
    public int End => Start + Length;

    public bool IsEmpty => Length == 0;


    private SourceRange(int start, int length)
    {
        Start = start;
        Length = length;
    }

    
    public static SourceRange FromBounds(int start, int end)
    {
        Guard.MustBe(end >= start);
        return new SourceRange(start, end - start);
    }

    public static SourceRange FromLength(int start, int length)
    {
        Guard.MustBe(length >= 0);
        return new SourceRange(start, length);
    }
    
    public static SourceRange FromTo(SourceRange first, SourceRange last)
    {
        Guard.InRange(first.Start <= last.End);
        return new SourceRange(first.Start, length: last.End - first.Start);
    }

    public static SourceRange EmptyAt(int position)
        => new(position, 0);
    
    public static SourceRange EmptyBefore(SourceRange range)
        => EmptyAt(range.Start);

    public static SourceRange EmptyAfter(SourceRange range)
        => EmptyAt(range.End);

    public static SourceRange Between(SourceRange left, SourceRange right)
    {
        Guard.InRange(left.End <= right.Start);
        return new SourceRange(left.End, length: right.Start - left.End);
    }
    
    
    public bool Contains(int index)
        => index >= Start && index < End;

    public bool Contains(SourceRange range)
        => range.Start >= Start && range.End <= End;

    /// <summary>
    /// Whether the given ranges are sequential without overlaps or gaps and range
    /// the entire <see cref="SourceRange"/>.
    /// If <paramref name="ranges"/> is empty, returns true iff this range is empty.
    /// </summary>
    public bool IsPartitionedBy(params IEnumerable<SourceRange> ranges)
    {
        var position = Start;
        foreach (var range in ranges)
        {
            if (range.Start != position)
                return false;
            position = range.End;
        }

        return position == End;
    }
    
    public override string ToString()
        => $"[{Start}, {End})";
}