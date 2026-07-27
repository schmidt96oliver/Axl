using System.Diagnostics;

namespace Axl.Compiler;

/// <summary>
/// Span of text inside <see cref="SourceFile"/>.
/// Never refers to <see cref="SourceView"/>.
/// 
/// Indices are UTF-16 code point indices as indexed by <see cref="string"/>.
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
        Debug.Assert(first >= 0);
        Debug.Assert(length >= 0);

        First = first;
        Length = length;
    }

    public bool Contains(int index)
        => index >= First && index < End;
    
    public override string ToString()
        => $"[{First}, {End})";


    public static SourceSpan FromTo(int first, int end)
    {
        Guard.InRange(first, first >= 0);
        Guard.InRange(end, end >= first);
        
        return new SourceSpan(first, end - first);
    }

    public static SourceSpan FromTo(SourceSpan first, SourceSpan last)
    {
        Guard.InRange(first.First, first.First <= last.End);
        return FromTo(first.First, last.End);
    }

    public static SourceSpan FromLength(int first, int length)
    {
        Guard.InRange(first, first >= 0);
        Guard.InRange(length, length >= 0);
        
        return new SourceSpan(first, length);
    }

    public static SourceSpan EmptyAt(int first)
        => new(first, 0);

}