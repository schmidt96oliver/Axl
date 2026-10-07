using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

/// <summary>
/// Two or more <see cref="FunSymbol"/>s with the same name.
/// </summary>
public sealed class FunGroupSymbol(string name, Symbol parent, ImmutableArray<FunSymbol> funs) 
    : Symbol(name, parent)
{
    public ImmutableArray<FunSymbol> Funs { get; } = funs.Length >= 2
        ? funs
        : throw new ArgumentException("Must have 2 or more funs.", nameof(funs));

    public FunSymbol? LookupFun(TypeSymbol? receiver, ImmutableArray<TypeSymbol> argumentTypes)
        => Funs.FirstOrDefault(fun => fun.ReceiverType == receiver &&
            fun.ParameterTypes.SequenceEqual(argumentTypes));

    public override bool IsPublic => Funs.Any(fun => fun.IsPublic);
}