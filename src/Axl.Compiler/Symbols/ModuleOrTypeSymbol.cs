using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public closed class ModuleOrTypeSymbol(string name) : Symbol(name)
{
    public abstract ImmutableArray<Symbol> Members { get; }
    
    public IntrinsicFunSymbol? LookupFun(string name, ImmutableArray<TypeSymbol> parameterTypes)
        => Members.OfType<IntrinsicFunSymbol>().SingleOrDefault(fun => fun.Name == name &&
                                                                       fun.ParameterTypes
                                                                           .SequenceEqual(parameterTypes));
    
    public Symbol? LookupMember(string name)
        => name is ""
            ? null
            : Members.SingleOrDefault(symbol => symbol.Name == name);
}