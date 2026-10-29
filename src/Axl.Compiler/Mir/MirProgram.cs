using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Mir;

public sealed class MirProgram(Func<FunSymbol, MutMirFunction> getMirFun, MutMirFunction entry)
{
    public MutMirFunction Entry { get; } = entry;

    public MutMirFunction GetMirFunction(FunSymbol fun)
    {
        if (fun.Body is not BoundBlock { HasError: false })
            throw new InvalidOperationException("Funs without body or with errors are not lowered.");

        return getMirFun(fun);
    }
}