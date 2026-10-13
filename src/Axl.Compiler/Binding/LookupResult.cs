using System.Collections.Immutable;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding;

public class LookupResult
{
    public LookupViability Viability { get; private set; } = LookupViability.NotFound;

    public ImmutableArray<Symbol> Symbols { get; private set; } = [];

    public Symbol? SingleViable 
        => Viability is LookupViability.Viable && Symbols.Length == 1 ? Symbols[0] : null;
    
    
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