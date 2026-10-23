using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class ErrorTypeSymbol : TypeSymbol
{
    public static readonly ErrorTypeSymbol Instance = new();

    /// <inheritdoc />
    public override bool CanBeReferencedByName => false;
    
    private ErrorTypeSymbol()
        : base(name: "???", owner: null, isPublic: true)
    {
        
    }

    public override ImmutableArray<Symbol> Members => [];
}