using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class TypeSymbol : Symbol
{
    public ImmutableArray<Symbol> Members { get; }
    
    public TypeSymbol(string name, Func<TypeSymbol, ImmutableArray<Symbol>> memberFactory) 
        : base(SymbolName.From(name))
    {
        Members = memberFactory(this);
    }

    public override string KindName => "native type";
}