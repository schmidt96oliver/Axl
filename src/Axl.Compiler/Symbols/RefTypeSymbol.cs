using System.Collections.Immutable;
using Axl.Compiler.Mir;

namespace Axl.Compiler.Symbols;

/// <summary>
/// Represents a reference (i.e. a pointer) to an inner type. Used only by
/// MIR.
/// </summary>
public sealed class RefTypeSymbol : TypeSymbol
{
    private static readonly Dictionary<TypeSymbol, RefTypeSymbol> _cache = [];
    
    public TypeSymbol Inner { get; }

    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;

    /// <inheritdoc />
    public override ImmutableArray<Symbol> Members => [];

    private RefTypeSymbol(TypeSymbol inner)
        : base(inner.Compilation, $"ref {inner.Name}", null, false)
    {
        Inner = inner;
    }

    public static RefTypeSymbol Get(TypeSymbol inner)
    {
        if (_cache.TryGetValue(inner, out var refType))
            return refType;

        refType = new RefTypeSymbol(inner);
        _cache[inner] = refType;
        return refType;
    }
}