using System.Collections.Immutable;

namespace Axl.Compiler.Semantics.Symbols;

/// <summary>
/// Represents a full or partial path consisting of <see cref="SymbolName"/>s
/// separated by dots.
/// </summary>
public readonly struct SymbolPath : IEquatable<SymbolPath>
{
    public ImmutableArray<SymbolName> Parts { get; }

    private SymbolPath(ImmutableArray<SymbolName> parts)
    {
        Parts = parts;
    }

    public static SymbolPath From(ImmutableArray<SymbolName> parts)
    {
        Guard.MustBe(!parts.IsDefaultOrEmpty);
        return new SymbolPath(parts);
    }

    public static SymbolPath From(params ReadOnlySpan<SymbolName> parts)
        => From(parts.ToImmutableArray());

    public static SymbolPath From(string pathText)
    {
        Guard.MustBe(!string.IsNullOrEmpty(pathText));

        var parts = ImmutableArray.CreateBuilder<SymbolName>();
        var textSpan = pathText.AsSpan();

        foreach (var partRange in textSpan.Split('.'))
        {
            var partText = textSpan[partRange];
            if (partText.IsEmpty || partText.IsWhiteSpace())
                throw new ArgumentException("Invalid path.", nameof(pathText));
            parts.Add(SymbolName.From(partText));
        }

        if (parts.Count == 0)
            throw new ArgumentException($"{nameof(pathText)} has no parts.", nameof(pathText));
        return new SymbolPath(parts.DrainToImmutable());
    }

    public static SymbolPath Combine(SymbolPath path, SymbolName extension)
        => new([.. path.Parts, extension]);


    public bool Equals(SymbolPath other)
    {
        if (other.Parts.Length != this.Parts.Length)
            return false;

        for (var i = 0; i < this.Parts.Length; i++)
        {
            if (this.Parts[i] != other.Parts[i])
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is SymbolPath other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        foreach (var part in Parts)
            hashCode.Add(part);
        return hashCode.ToHashCode();
    }
}