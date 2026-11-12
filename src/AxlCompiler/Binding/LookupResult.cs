using System.Collections.Immutable;
using AxlCompiler.Symbols;

namespace AxlCompiler.Binding;

public class LookupResult(LookupOptions options)
{
    public LookupViability Viability { get; private set; } = LookupViability.NotFound;

    public ImmutableArray<Symbol> Symbols { get; private set; } = [];

    public Symbol? SingleViable 
        => Viability is LookupViability.Viable && Symbols.Length == 1 ? Symbols[0] : null;

    public LookupOptions Options { get; } = options;


    public void MergeSingle(Symbol symbol, LookupViability viability)
    {
        if (viability > Viability)
        {
            Viability = viability;
            Symbols = [symbol];
        }
        else if (viability == Viability)
            Symbols = [.. Symbols, symbol];
    }

    public void MergeWith(LookupResult other)
    {
        if (other.Viability > Viability)
        {
            Viability = other.Viability;
            Symbols = other.Symbols;
        }
    }
}