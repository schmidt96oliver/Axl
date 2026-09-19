using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class TypeSymbol : Symbol
{
    public override SymbolKind Kind => SymbolKind.Type;
    
    
    public ImmutableArray<Symbol> Members { get; }
    
    public TypeSymbol(string name, Func<TypeSymbol, ImmutableArray<Symbol>> memberFactory) 
        : base(SymbolName.From(name))
    {
        Members = memberFactory(this);
    }

    public IntrinsicFunSymbol? LookupFun(SymbolName name, ImmutableArray<TypeSymbol> parameterTypes)
        => Members.OfType<IntrinsicFunSymbol>().SingleOrDefault(fun => fun.Name == name &&
                                                                       fun.ParemeterTypes
                                                                           .SequenceEqual(parameterTypes));
}