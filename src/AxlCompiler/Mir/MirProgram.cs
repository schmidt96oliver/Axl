using System.Collections.Frozen;
using System.Collections.Immutable;
using AxlCompiler.Symbols;

namespace AxlCompiler.Mir;

public sealed class MirProgram(MutMirFunction entry, FrozenDictionary<FunSymbol, MutMirFunction> mirFunctionsBySymbol)
{
    public MutMirFunction Entry { get; } = entry;
    public ImmutableArray<MutMirFunction> MirFunctions { get; } = mirFunctionsBySymbol.Values;

    public MutMirFunction GetMirFunction(FunSymbol fun)
        => mirFunctionsBySymbol[fun];

}