using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class NeverTypeSymbol : TypeSymbol
{
    public static readonly NeverTypeSymbol Instance = new();
    
    public override ImmutableArray<Symbol> Members => [];

    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;
    
    
    private NeverTypeSymbol()
        : base(name: "Never", owner: null, isPublic: true)
    {
        
    }
}