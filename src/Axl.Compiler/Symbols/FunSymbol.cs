using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class FunSymbol(
    string name,
    Intrinsic intrinsic,
    TypeSymbol? receiver,
    ImmutableArray<TypeSymbol> parameterTypes,
    TypeSymbol returnType)
    : Symbol(name)
{
    public override SymbolKind Kind => SymbolKind.Fun;

    /// <summary>
    /// The <see cref="TypeSymbol"/> this fun gets as an implicit
    /// 'self' argument. If this fun is a method, <see cref="Receiver"/>
    /// denotes the type it is a method of. <c>null</c>, if this
    /// method is free or a static member.
    /// </summary>
    public TypeSymbol? Receiver { get; } = receiver;
    
    public ImmutableArray<TypeSymbol> ParameterTypes { get; } = parameterTypes;
    public TypeSymbol ReturnType { get; } = returnType;
    
    public Intrinsic Intrinsic { get; } = intrinsic;
}