using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using AxlCompiler.Binding;
using AxlCompiler.Symbols;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace AxlCompiler.Mir.Interpreter;

using Environment = Dictionary<LocalId, object>;

public sealed class Miri
{
    private sealed class PanicException : Exception;

    private sealed class BranchLimitException : Exception;

    private sealed class CannotEvaluateConstException : Exception;
    
    private sealed class StructValue(TypeSymbol symbol)
    {
        public TypeSymbol Symbol { get; } = symbol;
        public readonly Dictionary<FieldSymbol, object> Fields = [];
    }

    private sealed record Reference(Environment Environment, Place Place);

    
    private readonly MirProgram _program;
    private readonly MiriContext _context;
    private int _branchCount = 0;

    private readonly StructValue _unitValue;
    private object _discard = new();

    private Miri(MirProgram program, MiriContext context)
    {
        _program = program;
        _context = context;
        _unitValue = new StructValue(context.UnitType);
    }

    public static MiriResult Run(MirProgram program, MiriContext context)
        => Run(program.Entry, program, context);
    
    public static MiriResult Run(MutMirFunction function, MirProgram program, MiriContext context)
    {
        Guard.MustBe(function.ParameterPlaces.Length == 0);
        
        var miri = new Miri(program, context);
        try
        {
            var miriReturnValue = miri.Call(function, []);
            return new MiriResult.Ok(miri.ToConstValue(miriReturnValue));
        }
        catch (PanicException)
        {
            return new MiriResult.Panicked();
        }
        catch (BranchLimitException)
        {
            return new MiriResult.HitBranchLimit();
        }
        catch (CannotEvaluateConstException)
        {
            return new MiriResult.CannotEvaluateConst();
        }
    }

    
    #region Values
    
    private object ToMiriValue(ConstValue constValue) => constValue switch
    {
        ConstStructValue constStructValue => ToMiriValue(constStructValue),
        bool or double or float or int or long or string => constValue.Value,
    };

    private StructValue ToMiriValue(ConstStructValue constStruct)
    {
        var value = new StructValue(constStruct.Struct);
        foreach (var kvp in constStruct.FieldValues)
            value.Fields.Add(kvp.Key, ToMiriValue(kvp.Value));
        return value;
    }

    private ConstValue ToConstValue(object miriValue) => miriValue switch
    {
        StructValue structValue => ToConstValue(structValue),
        bool b => b,
        double d => d,
        float f => f,
        int i => i,
        long l => l,
        string s => s,
        _ => throw new UnreachableException($"Unknown miri value type {miriValue.GetType().Name}.")
    };

    private ConstStructValue ToConstValue(StructValue structValue)
    {
        var dict = structValue.Fields.ToFrozenDictionary(kvp => kvp.Key, kvp => ToConstValue(kvp.Value));
        return new ConstStructValue(structValue.Symbol, dict);
    }

    private static object Copy(object value)
    {
        if (value is StructValue structValue)
        {
            var copy = new StructValue(structValue.Symbol);
            foreach (var kvp in structValue.Fields)
                copy.Fields.Add(kvp.Key, Copy(kvp.Value));
            return copy;
        }

        return value;
    }
    
    #endregion

    private object Call(MutMirFunction mirFun, ImmutableArray<object> arguments)
    {
        var environment = new Environment();
        for (var i = 0; i < mirFun.ParameterPlaces.Length; i++)
            environment[mirFun.ParameterPlaces[i].Id] = arguments[i];
        
        var block = mirFun.Entry;
        while (true)
        {
            if (_context.BranchLimit is int limit && _branchCount > limit)
                throw new BranchLimitException();
            _branchCount++;
            
            foreach (var instruction in block.Instructions)
                Execute(instruction, environment);

            switch (block.Terminator)
            {
                case Terminator.Goto @goto:
                    block = @goto.Block;
                    break;

                case Terminator.Branch branch:
                    var condition = Evaluate(branch.Condition, environment);
                    block = (bool)condition ? branch.True : branch.False;
                    break;

                case Terminator.Return:
                    return GetPlaceRef(mirFun.ReturnPlace, environment);

                default:
                    throw new UnreachableException($"Unknown terminator {block.Terminator?.GetType().Name ?? "null"}");
            }
        }
    }

    private void Execute(Instruction instruction, Environment environment)
    {
        switch (instruction)
        {
            case Instruction.Assign assign:
            {
                var value = Evaluate(assign.Value, environment);
                GetPlaceRef(assign.Target, environment) = value;
                break;
            }
            
            case Instruction.Call call:
            {
                var mirFun = _program.GetMirFunction(call.Fun);
                var value = Call(mirFun,
                [
                    .. call.Arguments.Select(a => Evaluate(a, environment))
                ]);
                GetPlaceRef(call.Target, environment) = value;
                break;
            }
            
            case Instruction.Operator intrinsic:
            {
                var value = EvaluateOperator(intrinsic.Kind,
                [
                    .. intrinsic.Arguments.Select(a => Evaluate(a, environment))
                ]);
                GetPlaceRef(intrinsic.Target, environment) = value;
                break;
            }
            
            case Instruction.StructInit structInit:
            {
                var value = new StructValue(structInit.Struct);
                foreach (var kvp in structInit.FieldValues)
                    value.Fields.Add(kvp.Key, Evaluate(kvp.Value, environment));
                GetPlaceRef(structInit.Target, environment) = value;
                break;
            }
            
            case Instruction.VmCall vmCall:
            {
                var value = EvaluateVmCall(vmCall.Kind,
                [
                    .. vmCall.Arguments.Select(a => Evaluate(a, environment))
                ]);
                GetPlaceRef(vmCall.Target, environment) = value;
                break;
            }
            
            default:
                throw new ArgumentOutOfRangeException(nameof(instruction));
        }
    }

    private static object EvaluateOperator(OperatorKind opKind, ImmutableArray<object> args)=> opKind switch
    {
        OperatorKind.AddWrappedI32 => (int)args[0] + (int)args[1],
        OperatorKind.SubWrappedI32 => (int)args[0] - (int)args[1],
        OperatorKind.DivUncheckedI32 => (int)args[0] / (int)args[1],
        OperatorKind.MulWrappedI32 => (int)args[0] * (int)args[1],
        OperatorKind.EqualsI32 => (int)args[0] == (int)args[1],
        OperatorKind.LessThanI32 => (int)args[0] < (int)args[1],

        OperatorKind.NegateI64 => -(long)args[0],
        OperatorKind.AddWrappedI64 => (long)args[0] + (long)args[1],
        OperatorKind.SubWrappedI64 => (long)args[0] - (long)args[1],
        OperatorKind.DivUncheckedI64 => (long)args[0] / (long)args[1],
        OperatorKind.MulWrappedI64 => (long)args[0] * (long)args[1],
        OperatorKind.EqualsI64 => (long)args[0] == (long)args[1],
        OperatorKind.LessThanI64 => (long)args[0] < (long)args[1],

        OperatorKind.AddF32 => (float)args[0] + (float)args[1],
        OperatorKind.SubtractF32 => (float)args[0] - (float)args[1],
        OperatorKind.DivideF32 => (float)args[0] / (float)args[1],
        OperatorKind.MultiplyF32 => (float)args[0] * (float)args[1],
        OperatorKind.EqualsF32 => (float)args[0] == (float)args[1],
        OperatorKind.LessThanF32 => (float)args[0] < (float)args[1],

        OperatorKind.AddF64 => (double)args[0] + (double)args[1],
        OperatorKind.SubtractF64 => (double)args[0] - (double)args[1],
        OperatorKind.DivideF64 => (double)args[0] / (double)args[1],
        OperatorKind.MultiplyF64 => (double)args[0] * (double)args[1],
        OperatorKind.EqualsF64 => (double)args[0] == (double)args[1],
        OperatorKind.LessThanF64 => (double)args[0] < (double)args[1],

        OperatorKind.NotBool => !(bool)args[0],
        OperatorKind.EqualsBool => (bool)args[0] == (bool)args[1],
        
        _ => throw new UnreachableException()
    };
    

    private object EvaluateVmCall(VmCallKind vmCallKind, ImmutableArray<object> args)
    {
        switch (vmCallKind)
        {
            case VmCallKind.Print:
                _context.StdOut?.Write(args[0]);
                return _unitValue;

            case VmCallKind.ConcatString:
                return string.Concat((string)args[0], (string)args[1]);

            case VmCallKind.Panic:
                var message = (string)args[0];
                _context.StdErr?.WriteLine($"Miri panicked: {message}");
                throw new PanicException();

            case VmCallKind.EqualsString:
                return (string)args[0] == (string)args[1];

            case VmCallKind.ToStringI32:
            case VmCallKind.ToStringI64:
                return args[0].ToString()!;

            case VmCallKind.ToStringBool:
                return (bool)args[0] ? "true" : "false";
            case VmCallKind.ToStringF32: return ((float)args[0]).ToString(CultureInfo.InvariantCulture);
            case VmCallKind.ToStringF64: return ((double)args[0]).ToString(CultureInfo.InvariantCulture);

            default:
                throw new UnreachableException();
        }
    }
    

    private ref object GetPlaceRef(Place place, Environment environment)
    {
        switch (place)
        {
            case Place.Local local:
                return ref CollectionsMarshal.GetValueRefOrAddDefault(environment, local.Id, out _)!;
            
            case Place.Field field:
                var receiver = GetPlaceRef(field.Receiver, environment);
                return ref CollectionsMarshal.GetValueRefOrNullRef(((StructValue)receiver).Fields, field.FieldSymbol);
            
            case Place.Deref deref:
                var @ref = (Reference)GetPlaceRef(deref.Reference, environment);
                return ref GetPlaceRef(@ref.Place, @ref.Environment);
            
            case Place.Discard:
                return ref _discard;
            
            default:
                throw new UnreachableException();
        }
    }

    private object Evaluate(Operand operand, Environment environment) => operand switch
    {
        Operand.Const @const => ToMiriValue(@const.Value),
        Operand.ConstEval constEval => EvaluateConstEval(constEval),
        Operand.Copy copy => Copy(GetPlaceRef(copy.Place, environment)),
        Operand.Ref @ref => new Reference(environment, @ref.Place),
        Operand.Unreachable or Operand.Error => throw new UnreachableException()
    };

    private object EvaluateConstEval(Operand.ConstEval constEval)
    {
        if (_context.TryGetConstEvaluation is null)
            return Call(_program.GetMirFunction(constEval.ConstSymbol.ValueFun), []);
        
        if (_context.TryGetConstEvaluation(constEval.ConstSymbol) is { } eval)
            return ToMiriValue(eval);

        throw new CannotEvaluateConstException();
    }
}