using Axl.Compiler.Symbols;

namespace Axl.Compiler.Mir;

public sealed class MirProgram(Compilation compilation)
{
    private readonly Dictionary<FunSymbol, MutMirFunction> _loweredFuns = [];

    public MutMirFunction GetMirFunction(FunSymbol fun)
    {
        if (_loweredFuns.TryGetValue(fun, out var mirFun))
            return mirFun;

        mirFun = Lowerer.Lower(fun, compilation);
        _loweredFuns[fun] = mirFun;
        return mirFun;
    }
}