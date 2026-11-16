using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using AxlCompiler.Symbols;

namespace AxlCompiler.Mir;

using Environment = Dictionary<LocalId, object>;

public enum RunResult
{
    Ok,
    Panic
}

public sealed class Miri
{
    private sealed class PanicException : Exception;
    
    private sealed class StructValue
    {
        public readonly Dictionary<FieldSymbol, object> Fields = [];
    }

    private sealed record Reference(Environment Environment, Place Place);


    private readonly MirProgram _program;
    private readonly TextWriter _stdOut;
    private readonly TextWriter _stdErr;
    private readonly object _empty = new StructValue();
    private object _discard = new();

    private Miri(MirProgram program, TextWriter stdOut, TextWriter stdErr)
    {
        _program = program;
        _stdOut = stdOut;
        _stdErr = stdErr;
    }

    public static RunResult Run(MirProgram program, TextWriter stdOut, TextWriter stdErr)
    {
        var miri = new Miri(program, stdOut, stdErr);
        try
        {
            miri.Call(program.Entry, []);
            return RunResult.Ok;
        }
        catch (PanicException)
        {
            return RunResult.Panic;
        }
    }

    private object Call(MutMirFunction mirFun, ImmutableArray<object> arguments)
    {
        var environment = new Environment();
        for (var i = 0; i < mirFun.ParameterPlaces.Length; i++)
            environment[mirFun.ParameterPlaces[i].Id] = arguments[i];
        
        var block = mirFun.Entry;
        while (true)
        {
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
                    throw new UnreachableException();
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
                var value = new StructValue();
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

        OperatorKind.NegateF32 => -(float)args[0],
        OperatorKind.AddF32 => (float)args[0] + (float)args[1],
        OperatorKind.SubtractF32 => (float)args[0] - (float)args[1],
        OperatorKind.DivideF32 => (float)args[0] / (float)args[1],
        OperatorKind.MultiplyF32 => (float)args[0] * (float)args[1],
        OperatorKind.EqualsF32 => (float)args[0] == (float)args[1],
        OperatorKind.LessThanF32 => (float)args[0] < (float)args[1],

        OperatorKind.NegateF64 => -(double)args[0],
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
                _stdOut.Write(args[0]);
                return _empty;

            case VmCallKind.ConcatString:
                return string.Concat((string)args[0], (string)args[1]);

            case VmCallKind.Panic:
                var message = (string)args[0];
                _stdErr.WriteLine($"PANIC: {message}");
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
        Operand.Const @const => @const.Value.Value,
        Operand.Copy copy => Copy(GetPlaceRef(copy.Place, environment)),
        Operand.Ref @ref => new Reference(environment, @ref.Place),
        Operand.Empty => _empty,
        Operand.Unreachable or Operand.Error => throw new UnreachableException()
    };

    private static object Copy(object value)
    {
        if (value is StructValue structValue)
        {
            var copy = new StructValue();
            foreach (var kvp in structValue.Fields)
                copy.Fields.Add(kvp.Key, Copy(kvp.Value));
            return copy;
        }

        return value;
    }
}