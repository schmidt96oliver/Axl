using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class TypeSymbol : ModuleOrTypeSymbol
{
    public override SymbolKind Kind => SymbolKind.Type;
    
    
    public override ImmutableArray<Symbol> Members { get; }
    
    public TypeSymbol(string name, Func<TypeSymbol, ImmutableArray<Symbol>> memberFactory) 
        : base(SymbolName.From(name))
    {
        Members = memberFactory(this);
    }
}