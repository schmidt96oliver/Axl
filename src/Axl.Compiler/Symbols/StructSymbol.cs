using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class StructSymbol : TypeSymbol
{
    /// <summary>
    /// Whether this structs memory representation is internal.
    /// </summary>
    public bool IsPrimitive { get; }
    
    public ImmutableArray<FieldSymbol> Fields => [.. Members.OfType<FieldSymbol>()];    

    public StructSymbol(string name, bool isPrimitive) 
        : base(name)
    {
        IsPrimitive = isPrimitive;
    }
}