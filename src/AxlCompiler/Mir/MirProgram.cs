using System.Collections.Frozen;
using System.Collections.Immutable;
using AxlCompiler.Symbols;

namespace AxlCompiler.Mir;

public sealed class MirProgram(MutMirFunction entry, Dictionary<FunSymbol, MutMirFunction> mirFunctionsBySymbol)
{
    public MutMirFunction Entry { get; } = entry;
    public IReadOnlyCollection<MutMirFunction> MirFunctions { get; } = mirFunctionsBySymbol.Values;

    public MutMirFunction GetMirFunction(FunSymbol fun)
        => mirFunctionsBySymbol[fun];

    public void RemoveUnusedFunctions()
    {
        var used = new HashSet<FunSymbol>();
        Collect(null, Entry);

        var unused = mirFunctionsBySymbol
            .Where(kvp => kvp.Value != Entry && !used.Contains(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var fun in unused)
            mirFunctionsBySymbol.Remove(fun);

        return;

        void Collect(FunSymbol? fun, MutMirFunction function)
        {
            if (fun is not null)
                used.Add(fun);
            
            foreach (var block in function.BasicBlocks)
            foreach (var call in block.Instructions.OfType<Instruction.Call>())
            {
                if (used.Add(call.Fun))
                    Collect(call.Fun, mirFunctionsBySymbol[call.Fun]);
            }
        }
    }
}