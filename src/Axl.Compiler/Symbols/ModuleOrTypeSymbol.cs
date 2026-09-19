using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public closed class ModuleOrTypeSymbol(SymbolName name) : Symbol(name)
{
    public abstract ImmutableArray<Symbol> Members { get; }
    
    public IntrinsicFunSymbol? LookupFun(SymbolName name, ImmutableArray<TypeSymbol> parameterTypes)
        => Members.OfType<IntrinsicFunSymbol>().SingleOrDefault(fun => fun.Name == name &&
                                                                       fun.ParemeterTypes
                                                                           .SequenceEqual(parameterTypes));
    
    public Symbol? LookupMember(SymbolName name)
        => name.IsEmpty
            ? null
            : Members.SingleOrDefault(symbol => symbol.Name == name);
}