using Axl.Compiler.Symbols;

namespace Axl.Compiler.Lowering;

public sealed class MirProgram(Compilation compilation)
{
    private readonly Dictionary<FunSymbol, MirFunction> _loweredFuns = [];

    public MirFunction GetMirFunction(FunSymbol fun)
    {
        if (_loweredFuns.TryGetValue(fun, out var mirFun))
            return mirFun;

        mirFun = Lowerer.Lower(fun, compilation);
        _loweredFuns[fun] = mirFun;
        return mirFun;
    }
}