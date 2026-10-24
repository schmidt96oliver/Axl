using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Lowering;

using Environment = Dictionary<LocalId, object>;

public sealed class Miri
{
    private sealed class StructValue
    {
        public readonly Dictionary<FieldSymbol, object> Fields = [];
    }

    private sealed record Reference(Environment Environment, Place Place);


    private readonly MirProgram _program;
    private readonly TextWriter _output;
    private readonly object _empty = new StructValue();
    private object _discard = new();

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

        var environment = new Environment();

        for (var paramIndex = 0; paramIndex < mirFun.Parameters.Length; paramIndex++)
        {
            var place = mirFun.GetParameterPlace(mirFun.Parameters[paramIndex]);
            environment[place.Id] = arguments[paramIndex];
        }

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

                case Terminator.Return @return:
                    var value = Evaluate(@return.Value, environment);
                    return value;

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
                var value = Call(call.Fun,
                [
                    .. call.Arguments.Select(a => Evaluate(a, environment))
                ]);
                GetPlaceRef(call.Target, environment) = value;
                break;
            }
            
            case Instruction.Intrinsic intrinsic:
            {
                var value = EvaluateIntrinsic(intrinsic.Kind,
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
        Operand.Unreachable => throw new UnreachableException(),
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