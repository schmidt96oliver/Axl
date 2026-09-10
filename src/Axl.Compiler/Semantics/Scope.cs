using Axl.Compiler.Semantics.Symbols;

namespace Axl.Compiler.Semantics;

public sealed class Scope(Scope? parent = null)
{
    private readonly List<Symbol> _declaredSymbols = [];
    
    public Scope? Parent { get; } = parent;

    
    public Symbol? Lookup(SymbolName name)
    {
        // Last declared symbol is first, since it might shadow
        // symbols declared before.
        
        return _declaredSymbols.LastOrDefault(symbol => symbol.Name == name)
               ?? Parent?.Lookup(name);
    }

    public void Declare(Symbol symbol)
    {
        _declaredSymbols.Add(symbol);
    }
}