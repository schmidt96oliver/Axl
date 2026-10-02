using Axl.Compiler.Symbols;
using Axl.Compiler.Text;

namespace Axl.Compiler.Binding;

/// <summary>
/// This is a temporary solution for a working LSP. Keeping
/// side tables is easier, but adds unnecessary churn and allocation
/// in the binder. To be reworked
/// </summary>
public sealed class SemanticSideTable
{
    private readonly Dictionary<SourceLocation, Symbol> _resolvedSymbols = [];

    public void AddResolvedSymbol(SourceLocation location, Symbol? symbol)
    {
        if (symbol is null) return;
        _resolvedSymbols[location] = symbol;
    }

    public Symbol? TryGetSymbol(SourceLocation location)
        => _resolvedSymbols.GetValueOrDefault(location);
}