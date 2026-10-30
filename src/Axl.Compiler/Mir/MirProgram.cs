using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Mir;

public sealed class MirProgram(Func<FunDefinitionSymbol, MutMirFunction> getMirFun, MutMirFunction entry)
{
    public MutMirFunction Entry { get; } = entry;

    public MutMirFunction GetMirFunction(FunDefinitionSymbol fun)
    {
        if (fun.Body is not BoundBlock { HasError: false })
            throw new InvalidOperationException("Funs without body or with errors are not lowered.");

        return getMirFun(fun);
    }
}