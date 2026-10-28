using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Mir;

public sealed class MirProgram(Compilation compilation)
{
    private readonly Dictionary<FunSymbol, MutMirFunction> _loweredFuns = [];

    /// <summary>
    /// Returns the mir for source-declared funs without errors. <c>null</c>,
    /// otherwise.
    /// </summary>
    public MutMirFunction? TryGetMirFunction(FunSymbol fun)
    {
        if (fun.Body is not BoundBlock { HasError: false })
            return null;
        
        if (_loweredFuns.TryGetValue(fun, out var mirFun))
            return mirFun;

        mirFun = Lowerer.Lower(fun, compilation);
        _loweredFuns[fun] = mirFun;
        return mirFun;
    }
}