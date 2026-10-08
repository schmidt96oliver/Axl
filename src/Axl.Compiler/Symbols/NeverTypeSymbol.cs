using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class NeverTypeSymbol : TypeSymbol
{
    public static readonly NeverTypeSymbol Instance = new();
    
    public override ImmutableArray<Symbol> Members => [];
    
    
    private NeverTypeSymbol()
        : base(name: "Never", owner: null, isPublic: true)
    {
        
    }
}