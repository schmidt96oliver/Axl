using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class IntrinsicFunSymbol(
    string name,
    Intrinsic intrinsic,
    ImmutableArray<TypeSymbol> parameterTypes,
    TypeSymbol returnType)
    : Symbol(name)
{
    public override SymbolKind Kind => SymbolKind.Fun;

    
    public Intrinsic Intrinsic { get; } = intrinsic;
    public ImmutableArray<TypeSymbol> ParameterTypes { get; } = parameterTypes;
    public TypeSymbol ReturnType { get; } = returnType;
}