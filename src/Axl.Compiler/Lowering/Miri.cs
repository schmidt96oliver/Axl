using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Lowering;

public sealed class Miri
{
    private sealed class StructValue
    {
        public readonly Dictionary<FieldSymbol, object> Fields = [];
    }


    private readonly MirProgram _program;
    private readonly TextWriter _output;

    private Dictionary<LocalId, object> _locals = [];
    private readonly object _empty = new StructValue();

    private Miri(MirProgram program, TextWriter output)
    {
        _program = program;
        _output = output;
    }

    public static void Run(MirProgram program, FunSymbol entry, TextWriter output)
    {
        var miri = new Miri(program, output);
        miri.Call(entry, []);
    }

    private object Call(FunSymbol fun, ImmutableArray<object> arguments)
    {
        var mirFun = _program.GetMirFunction(fun);

        var prevLocals = _locals;
        _locals = new Dictionary<LocalId, object>();

        var firstArgument = 0;
        if (mirFun.ReceiverPlace is not null)
        {
            _locals[mirFun.ReceiverPlace.Id] = arguments[firstArgument];
            firstArgument++;
        }

        for (var paramIndex = 0; paramIndex < mirFun.Parameters.Count; paramIndex++)
            _locals[mirFun.Parameters[paramIndex].Id] = arguments[paramIndex + firstArgument];

        var block = mirFun.Entry;
        while (true)
        {
            foreach (var instruction in block.Instructions)
                Execute(instruction);

            switch (block.Terminator)
            {
                case Terminator.Goto @goto:
                    block = @goto.Block;
                    break;

                case Terminator.Branch branch:
                    var condition = Evaluate(branch.Condition);
                    block = (bool)condition ? branch.True : branch.False;
                    break;

                case Terminator.Return @return:
                    var value = Evaluate(@return.Value);
                    _locals = prevLocals;
                    return value;

                default:
                    throw new UnreachableException();
            }
        }
    }

    private void Execute(Instruction instruction)
    {
        switch (instruction)
        {
            case Instruction.Assign assign:
                GetPlaceRef(assign.Target) = Evaluate(assign.Value);
                break;
            
            case Instruction.Call call:
                GetPlaceRef(call.Target) = Call(call.Fun, [.. call.Arguments.Select(Evaluate)]);
                break;
            
            case Instruction.Intrinsic intrinsic:
                GetPlaceRef(intrinsic.Target) = EvaluateIntrinsic(intrinsic.Kind, [.. intrinsic.Arguments.Select(Evaluate)]);
                break;
            
            case Instruction.StructInit structInit:
                var @struct = new StructValue();
                foreach (var kvp in structInit.FieldValues)
                    @struct.Fields.Add(kvp.Key, Evaluate(kvp.Value));
                GetPlaceRef(structInit.Target) = @struct;
                break;
            
            case Instruction.VmCall vmCall:
                GetPlaceRef(vmCall.Target) = EvaluateVmCall(vmCall.Kind, [.. vmCall.Arguments.Select(Evaluate)]);
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(instruction));
        }
    }

    private static object EvaluateIntrinsic(Intrinsic intrinsic, ImmutableArray<object> args)=> intrinsic switch
    {
        Intrinsic.NegateI32 => -(int)args[0],
        Intrinsic.AddI32 => (int)args[0] + (int)args[1],
        Intrinsic.SubtractI32 => (int)args[0] - (int)args[1],
        Intrinsic.DivideI32 => (int)args[0] / (int)args[1],
        Intrinsic.MultiplyI32 => (int)args[0] * (int)args[1],
        Intrinsic.EqualsI32 => (int)args[0] == (int)args[1],
        Intrinsic.LessThanI32 => (int)args[0] < (int)args[1],

        Intrinsic.NegateI64 => -(long)args[0],
        Intrinsic.AddI64 => (long)args[0] + (long)args[1],
        Intrinsic.SubtractI64 => (long)args[0] - (long)args[1],
        Intrinsic.DivideI64 => (long)args[0] / (long)args[1],
        Intrinsic.MultiplyI64 => (long)args[0] * (long)args[1],
        Intrinsic.EqualsI64 => (long)args[0] == (long)args[1],
        Intrinsic.LessThanI64 => (long)args[0] < (long)args[1],

        Intrinsic.NegateF32 => -(float)args[0],
        Intrinsic.AddF32 => (float)args[0] + (float)args[1],
        Intrinsic.SubtractF32 => (float)args[0] - (float)args[1],
        Intrinsic.DivideF32 => (float)args[0] / (float)args[1],
        Intrinsic.MultiplyF32 => (float)args[0] * (float)args[1],
        Intrinsic.EqualsF32 => (float)args[0] == (float)args[1],
        Intrinsic.LessThanF32 => (float)args[0] < (float)args[1],

        Intrinsic.NegateF64 => -(double)args[0],
        Intrinsic.AddF64 => (double)args[0] + (double)args[1],
        Intrinsic.SubtractF64 => (double)args[0] - (double)args[1],
        Intrinsic.DivideF64 => (double)args[0] / (double)args[1],
        Intrinsic.MultiplyF64 => (double)args[0] * (double)args[1],
        Intrinsic.EqualsF64 => (double)args[0] == (double)args[1],
        Intrinsic.LessThanF64 => (double)args[0] < (double)args[1],

        Intrinsic.NotBool => !(bool)args[0],
        Intrinsic.EqualsBool => (bool)args[0] == (bool)args[1],

        Intrinsic.EqualsString => (string)args[0] == (string)args[1],

        Intrinsic.ToStringI32 or Intrinsic.ToStringI64 => args[0].ToString()!,
        Intrinsic.ToStringBool => (bool)args[0] ? "true" : "false",
        Intrinsic.ToStringF32 => ((float)args[0]).ToString(CultureInfo.InvariantCulture)!,
        Intrinsic.ToStringF64 => ((double)args[0]).ToString(CultureInfo.InvariantCulture)!,

        _ => throw new UnreachableException()
    };
    

    private object EvaluateVmCall(Intrinsic intrinsic, ImmutableArray<object> args)
    {
        switch (intrinsic)
        {
            case Intrinsic.Print:
                _output.Write(args[0]);
                return _empty;
            
            case Intrinsic.ConcatString:
                return string.Concat((string)args[0], (string)args[1]);
            
            default:
                throw new UnreachableException();
        }
    }
    

    private ref object GetPlaceRef(Place place)
    {
        switch (place)
        {
            case Place.Local local:
                return ref CollectionsMarshal.GetValueRefOrAddDefault(_locals, local.Id, out _)!;
            
            case Place.Field field:
                var receiver = GetPlaceRef(field.Receiver);
                return ref CollectionsMarshal.GetValueRefOrNullRef(((StructValue)receiver).Fields, field.FieldSymbol);
            
            default:
                throw new UnreachableException();
        }
    }

    private object Evaluate(Operand operand) => operand switch
    {
        Operand.Const @const => @const.Value.Value,
        Operand.Copy copy => Copy(GetPlaceRef(copy.Place)),
        Operand.EmptyStructConst => _empty,
        Operand.Unreachable => throw new UnreachableException(),
    };

    private object Copy(object value)
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