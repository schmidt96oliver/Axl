using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class IntrinsicFunSymbol(
    SymbolName name,
    Intrinsic intrinsic,
    ImmutableArray<TypeSymbol> paremeterTypes,
    TypeSymbol returnType)
    : Symbol(name)
{
    public override string KindName => "intrinsic fun";
    
    public Intrinsic Intrinsic { get; } = intrinsic;
    public ImmutableArray<TypeSymbol> ParemeterTypes { get; } = paremeterTypes;
    public TypeSymbol ReturnType { get; } = returnType;
}