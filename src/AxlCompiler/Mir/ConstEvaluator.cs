using System.Collections.Frozen;
using System.Diagnostics;
using AxlCompiler.Binding;
using AxlCompiler.Diagnostics;
using AxlCompiler.Mir.Interpreter;
using AxlCompiler.Symbols;

namespace AxlCompiler.Mir;

public sealed class ConstEvaluator
{
    private readonly Dictionary<ConstSymbol, ConstValue?> _values = [];
    private readonly Stack<ConstSymbol> _inProgress = [];
    private readonly MirProgram _program;
    private readonly Compilation _compilation;
    private readonly DiagnosticBag _diagnosticBag;

    private ConstEvaluator(MirProgram program, Compilation compilation, DiagnosticBag diagnosticBag)
    {
        _program = program;
        _compilation = compilation;
        _diagnosticBag = diagnosticBag;
    }
    
    public static void EvaluateAll(MirProgram program, Compilation compilation, DiagnosticBag diagnosticBag)
    {
        var evaluator = new ConstEvaluator(program, compilation, diagnosticBag);
        foreach (var function in program.MirFunctions)
            evaluator.Evaluate(function);
        program.RemoveUnusedFunctions();
    }

    private ConstValue? EvaluateConst(ConstSymbol symbol)
    {
        if (_values.TryGetValue(symbol, out var value))
            return value;

        if (_inProgress.Contains(symbol))
        {
            //TODO: Handle error!
            _values[symbol] = null;
            return null;
        }
        
        _inProgress.Push(symbol);

        var miriContext = new MiriContext(_compilation.BaseLibrary.Unit, Console.Out, Console.Error,
            BranchLimit: 1000, EvaluateConst);
        var result = Miri.Run(_program.GetMirFunction(symbol.ValueFun), _program, miriContext);
        value = null;
        
        switch (result)
        {
            case MiriResult.Ok ok:
                value = ok.Value;
                break;
            
            case MiriResult.CannotEvaluateConst cannotEvaluateConst:
                value = null;
                break;
            
            case MiriResult.HitBranchLimit hitBranchLimit:
                //TODO: Report error
                value = null;
                break;
            
            
            case MiriResult.Panicked panicked:
                //TODO: Report error
                value = null;
                break;
        }
        
        var popped = _inProgress.Pop();
        Debug.Assert(popped == symbol);

        _values[symbol] = value;
        return value;
    }
    
    
    private void Evaluate(MutMirFunction mirFunction)
    {
        foreach (var block in mirFunction.BasicBlocks)
            for (var i = 0; i < block.Instructions.Count; i++)
                block.ReplaceAt(i, ReplaceInstruction(block.Instructions[i]));
    }

    private Instruction ReplaceInstruction(Instruction original) => original switch
    {
        Instruction.Assign assign => new Instruction.Assign(assign.Target, ReplaceOperand(assign.Value), assign.Syntax),
        Instruction.Call call => new Instruction.Call(call.Fun, call.Target, [.. call.Arguments.Select(ReplaceOperand)], call.Syntax),
        Instruction.Operator op => new Instruction.Operator(op.Kind, op.Target,
            [.. op.Arguments.Select(ReplaceOperand)], op.Syntax),
        Instruction.StructInit structInit => new Instruction.StructInit(structInit.Struct,
            structInit.Target, structInit.FieldValues.ToFrozenDictionary(kvp => kvp.Key, kvp => ReplaceOperand(kvp.Value)), structInit.Syntax),
        Instruction.VmCall vmCall => new Instruction.VmCall(vmCall.Kind, vmCall.Target,
            [.. vmCall.Arguments.Select(ReplaceOperand)], vmCall.Syntax),
    };

    private Operand ReplaceOperand(Operand original)
    {
        if (original is not Operand.ConstEval constEval)
            return original;
        
        if (EvaluateConst(constEval.ConstSymbol) is not { } value)
            return new Operand.Error(_compilation.BaseLibrary.Error);
        
        return new Operand.Const(value, constEval.Type, constEval.Syntax);
    }
}