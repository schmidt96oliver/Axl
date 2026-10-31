using System.Collections.Frozen;
using System.Diagnostics;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Mir;

public class Monomorphizer
{
    private readonly Queue<FunSymbol> _worklist = [];
    private readonly Dictionary<FunSymbol, MutMirFunction> _instantiatedMirFuns = [];
    private readonly Func<FunDefinitionSymbol, MutMirFunction> _lowerDefinition;

    
    private Monomorphizer(Func<FunDefinitionSymbol, MutMirFunction> lowerDefinition)
    {
        _lowerDefinition = lowerDefinition;
    }

    public static MirProgram Monomorphize(FunSymbol entryFunSymbol, Func<FunDefinitionSymbol, MutMirFunction> lowerDefinition)
    {
        Guard.MustBe(!entryFunSymbol.IsGeneric);
        
        var monomorphizer = new Monomorphizer(lowerDefinition);
        monomorphizer._worklist.Enqueue(entryFunSymbol);
        
        while (monomorphizer._worklist.Count > 0)
        {
            var fun = monomorphizer._worklist.Dequeue();
            monomorphizer.Instantiate(fun);
        }

        return new MirProgram(monomorphizer._instantiatedMirFuns[entryFunSymbol],
            monomorphizer._instantiatedMirFuns.ToFrozenDictionary());
    }

    private void AddToWorklist(FunSymbol funSymbol)
    {
        if (_instantiatedMirFuns.ContainsKey(funSymbol) || _worklist.Contains(funSymbol))
            return;
        _worklist.Enqueue(funSymbol);
    }

    private void Instantiate(FunSymbol funSymbol)
    {
        // If the fun is not generic, we don't need to instantiate it, technically.
        // But just to test the monomorphizer, we do it anyway and bugs
        // will appear in any corpus test.
        
        if (_instantiatedMirFuns.ContainsKey(funSymbol))
            return;
        
        var originalMirFun = _lowerDefinition(funSymbol.OriginalDefinition);
        var typeMap = TypeMap.StrictFrom(funSymbol.TypeParameters, funSymbol.TypeArguments);
        
        var instantiatedMirFun = new MutMirFunction(funSymbol.FullName,
            InstantiateLocal(originalMirFun.ReturnPlace, null, typeMap),
            [.. originalMirFun.ParameterPlaces.Select(p => InstantiateLocal(p, null, typeMap))]
        );
        
        _instantiatedMirFuns[funSymbol] = instantiatedMirFun;

        foreach (var originalBlock in originalMirFun.BasicBlocks)
            InstantiateBlock(originalBlock, instantiatedMirFun, typeMap);
    }

    private MutBasicBlock InstantiateBlock(MutBasicBlock original, MutMirFunction instantiatedMirFun, TypeMap map)
    {
        MutBasicBlock block;
        
        // The entry block is added to MirFunction in its constructor, so we cannot
        // add it after the fact.
        if (original == original.MirFunction.Entry)
        {
            block = instantiatedMirFun.Entry;
            if (block.Instructions.Count > 0)
            {
                // Apparently, we have instantiated it, so we don't do
                // it again.
                return block;
            }
        }
        else
        {
            if (instantiatedMirFun.BasicBlocks.FirstOrDefault(b => b.Id == original.Id) is { } instantiatedBlock)
                return instantiatedBlock;

            block = new MutBasicBlock(original.Id, instantiatedMirFun, original.DebugLabel);
            instantiatedMirFun.AddBasicBlock(block);
        }

        foreach (var originalInstruction in original.Instructions)
            block.Add(InstantiateInstruction(originalInstruction, instantiatedMirFun, map));

        block.Terminator = original.Terminator is not null
            ? InstantiateTerminator(original.Terminator, instantiatedMirFun, map)
            : null;
        return block;
    }

    private Instruction InstantiateInstruction(Instruction original, MutMirFunction instantiatedMirFun, TypeMap map)
        => original switch
        {
            Instruction.Assign originalAssign => new Instruction.Assign(
                InstantiatePlace(originalAssign.Target, instantiatedMirFun, map),
                InstantiateOperand(originalAssign.Value, instantiatedMirFun, map),
                originalAssign.Syntax),

            Instruction.Operator originalOperator => new Instruction.Operator(
                originalOperator.Kind,
                InstantiatePlace(originalOperator.Target, instantiatedMirFun, map),
                [.. originalOperator.Arguments.Select(arg => InstantiateOperand(arg, instantiatedMirFun, map))],
                originalOperator.Syntax),

            Instruction.Call originalCall => InstantiateCall(originalCall, instantiatedMirFun, map),

            Instruction.StructInit originalStructInit => new Instruction.StructInit(
                originalStructInit.Struct,
                InstantiatePlace(originalStructInit.Target, instantiatedMirFun, map),
                originalStructInit.FieldValues
                    .ToDictionary(kvp => kvp.Key, kvp => InstantiateOperand(kvp.Value, instantiatedMirFun, map))
                    .ToFrozenDictionary(),
                originalStructInit.Syntax),

            Instruction.VmCall originalVmCall => new Instruction.VmCall(
                originalVmCall.Kind,
                InstantiatePlace(originalVmCall.Target, instantiatedMirFun, map),
                [.. originalVmCall.Arguments.Select(arg => InstantiateOperand(arg, instantiatedMirFun, map))],
                originalVmCall.Syntax)
        };

    private Instruction.Call InstantiateCall(Instruction.Call original, MutMirFunction instantiatedMirFun, TypeMap map)
    {
        // Substitute type arguments
        var fun = original.Fun.ConstructWith(
        [
            .. original.Fun.TypeArguments.Select(typeArg => typeArg.Substitute(map))
        ]);
        
        AddToWorklist(fun);

        return new Instruction.Call(fun,
            InstantiatePlace(original.Target, instantiatedMirFun, map),
            [.. original.Arguments.Select(arg => InstantiateOperand(arg, instantiatedMirFun, map))],
            original.Syntax);
    }

    private Place InstantiatePlace(Place original, MutMirFunction instantiatedMirFun, TypeMap map) => original switch
    {
        Place.Local originalLocal => InstantiateLocal(originalLocal, instantiatedMirFun, map),

        Place.Field originalField => new Place.Field(
            InstantiatePlace(originalField.Receiver, instantiatedMirFun, map),
            originalField.FieldSymbol,
            originalField.Syntax),

        Place.Deref originalDeref => new Place.Deref(
            InstantiatePlace(originalDeref.Reference, instantiatedMirFun, map),
            originalDeref.Syntax),

        Place.Discard => original,
        Place.Error => throw new UnreachableException("Cannot monomorphize errors.")
    };

    /// <param name="instantiatedMirFun">
    /// <c>null</c>, if the local gets instantiated before creation
    /// of the mir fun. It is then the callers responsibility to add it to the mir fun.
    /// </param>
    private Place.Local InstantiateLocal(Place.Local original, MutMirFunction? instantiatedMirFun, TypeMap map)
    {
        if (instantiatedMirFun?.Locals.FirstOrDefault(l => l.Id == original.Id) is { } local)
            return local;

        local = new Place.Local(original.Id, original.Kind, original.Type.Substitute(map), original.Syntax);
        instantiatedMirFun?.AddLocal(local);
        return local;
    }

    private Operand InstantiateOperand(Operand original, MutMirFunction instantiatedMirFun, TypeMap map)
        => original switch
        {
            Operand.Const originalConst => new Operand.Const(
                originalConst.Value,
                originalConst.Type.Substitute(map),
                originalConst.Syntax),

            Operand.Copy originalCopy => new Operand.Copy(
                InstantiatePlace(originalCopy.Place, instantiatedMirFun, map),
                originalCopy.Syntax),

            Operand.Empty originalEmpty => new Operand.Empty(
                originalEmpty.Type.Substitute(map),
                originalEmpty.Syntax),

            Operand.Ref originalRef => new Operand.Ref(
                InstantiatePlace(originalRef.Place, instantiatedMirFun, map),
                originalRef.Type.Substitute(map),
                originalRef.Syntax),

            Operand.Unreachable => original,
            Operand.Error => throw new UnreachableException("Errors cannot be monomorphized.")
        };

    private Terminator InstantiateTerminator(Terminator original, MutMirFunction instantiatedMirFun, TypeMap map)
        => original switch
        {
            Terminator.Branch originalBranch => new Terminator.Branch(
                InstantiateOperand(originalBranch.Condition, instantiatedMirFun, map),
                InstantiateBlock(originalBranch.True, instantiatedMirFun, map),
                InstantiateBlock(originalBranch.False, instantiatedMirFun, map),
                originalBranch.Syntax),

            Terminator.Goto originalGoto => new Terminator.Goto(
                InstantiateBlock(originalGoto.Block, instantiatedMirFun, map),
                originalGoto.Syntax),

            Terminator.Return or Terminator.Unreachable => original,
            Terminator.MissingReturn => throw new UnreachableException("Errors cannot be monomorphized."),
        };
}