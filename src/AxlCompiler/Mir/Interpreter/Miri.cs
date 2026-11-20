using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using AxlCompiler.Binding;
using AxlCompiler.Symbols;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace AxlCompiler.Mir.Interpreter;

public sealed class Miri
{
    private sealed class PanicException : Exception;

    private sealed class OutOfFuelException : Exception;

    private sealed class CannotEvaluateConstException : Exception;

    private sealed class StructValue(TypeSymbol symbol)
    {
        public TypeSymbol Symbol { get; } = symbol;
        public readonly Dictionary<FieldSymbol, object> Fields = [];
    }

    private sealed class ClassValue(TypeSymbol symbol)
    {
        public TypeSymbol Symbol { get; } = symbol;
        public readonly Dictionary<FieldSymbol, object> Fields = [];
    }

    private sealed class BufferValue(object[] array)
    {
        public object?[] Array { get; } = array;
    }

    private sealed record ClassHandle(HeapPointerRoot HeapPointerRoot);
    
    private sealed record HeapPointerRoot(int Index);

    private sealed record LocalPointerRoot
    {
        public LocalPointerRoot(Frame Frame, Place.Local Local)
        {
            Guard.MustBe(Frame.IsDefined(Local));
            this.Frame = Frame;
            this.Local = Local;
        }

        public Frame Frame { get; }
        public Place.Local Local { get; }
    }

    private sealed record BufferPointerRoot(HeapPointerRoot Buffer, int Index);

    private union PointerRoot(HeapPointerRoot, LocalPointerRoot, BufferPointerRoot);

    private sealed record Pointer(PointerRoot Root, ImmutableArray<FieldSymbol> Path)
    {
        public Pointer Extend(FieldSymbol field)
            => new(Root, [.. Path, field]);
    }


    private readonly MirProgram _program;
    private readonly MiriContext _context;
    private int? _fuel;

    private readonly List<object> _heap = [];
    private readonly StructValue _unitValue;
    private object _discard = new();

    private Miri(MirProgram program, MiriContext context)
    {
        _program = program;
        _context = context;
        _unitValue = new StructValue(context.UnitType);
        _fuel = context.Fuel;
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
            var constReturnValue = miri.ToConstValue(miriReturnValue);
            if (constReturnValue is null)
                return new MiriResult.HeapAllocatedConst();
            
            return new MiriResult.Ok(constReturnValue.Value);
        }
        catch (PanicException)
        {
            return new MiriResult.Panicked();
        }
        catch (OutOfFuelException)
        {
            return new MiriResult.OutOfFuel();
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

    private ConstValue? ToConstValue(object miriValue) => miriValue switch
    {
        StructValue structValue => ToConstValue(structValue),
        bool b => b,
        double d => d,
        float f => f,
        int i => i,
        long l => l,
        string s => s,
        ClassValue or ClassHandle or Pointer => null,
        _ => throw new UnreachableException($"Unknown miri value type {miriValue.GetType().Name}.")
    };

    private ConstValue? ToConstValue(StructValue structValue)
    {
        var dict = structValue.Fields.ToDictionary(kvp => kvp.Key, kvp => ToConstValue(kvp.Value));
        if (dict.Values.Any(v => v is null)) return null;
        
        return new ConstStructValue(structValue.Symbol, dict.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value!.Value));
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

    private void UseFuel()
    {
        _fuel--;
        if (_fuel is <= 0)
            throw new OutOfFuelException();
    }

    private ClassHandle AllocateClass(object value)
    {
        _heap.Add(value);
        return new ClassHandle(new HeapPointerRoot(_heap.Count - 1));
    }

    /// <summary>
    /// Returns ref to <c>null</c>, if the place has not yet been assigned to.
    /// </summary>
    private ref object? GetPlaceRef(Place place, Frame frame)
    {
        switch (place)
        {
            case Place.Local local:
                return ref frame.GetRef(local);

            case Place.Field field:
            {
                var receiver = ReadPlace(field.Receiver, frame);

                if (receiver is StructValue structReceiver)
                    return ref CollectionsMarshal.GetValueRefOrNullRef(structReceiver.Fields, field.FieldSymbol)!;

                if (receiver is ClassHandle { HeapPointerRoot: var classRoot })
                {
                    var classValue = (ClassValue)_heap[classRoot.Index];
                    return ref CollectionsMarshal.GetValueRefOrNullRef(classValue.Fields, field.FieldSymbol)!;
                }

                throw new UnreachableException();
            }

            case Place.Deref deref:
            {
                if (ReadPlace(deref.Reference, frame) is not Pointer pointer)
                    throw new InvalidOperationException($"No {nameof(Pointer)} in a ref field.");

                return ref DerefPointer(pointer);
            }

            case Place.Discard:
                return ref _discard!;

            default:
                throw new UnreachableException();
        }
    }

    private ref object? DerefPointer(Pointer pointer)
    {
        if (pointer.Path.Length == 0)
        {
            if (pointer.Root is BufferPointerRoot bufferRoot)
                return ref ((BufferValue)_heap[bufferRoot.Buffer.Index]).Array[bufferRoot.Index];
            
            if (pointer.Root is LocalPointerRoot localRoot)
                return ref localRoot.Frame.GetRef(localRoot.Local);
            
            throw new InvalidOperationException("Invalid miri pointer.");
        }

        var current = pointer.Root switch
        {
            HeapPointerRoot heapRoot => _heap[heapRoot.Index],
            LocalPointerRoot localRoot => localRoot.Frame.GetRef(localRoot.Local) ?? throw new InvalidOperationException(),
            BufferPointerRoot bufferRoot => ((BufferValue)_heap[bufferRoot.Buffer.Index]).Array[bufferRoot.Index],
        };
        
        for (var i = 0; i < pointer.Path.Length - 1; i++)
        {
            current = current switch
            {
                ClassValue classValue => classValue.Fields[pointer.Path[i]],
                StructValue structValue => structValue.Fields[pointer.Path[i]],
                _ => throw new InvalidOperationException("Invalid miri pointer.")
            };
        }

        if (current is StructValue currentStruct)
            return ref CollectionsMarshal.GetValueRefOrNullRef(currentStruct.Fields, pointer.Path[^1])!;
        if (current is ClassValue currentClass)
            return ref CollectionsMarshal.GetValueRefOrNullRef(currentClass.Fields, pointer.Path[^1])!;

        throw new InvalidOperationException("Invalid miri pointer.");
    }


    private Pointer PointerTo(Place place, Frame frame) =>
        place switch
        {
            Place.Local local => new Pointer(new LocalPointerRoot(frame, local), []),
            Place.Field field => ReadPlace(field.Receiver, frame) is ClassHandle receiverClassHandle
                    ? new Pointer(receiverClassHandle.HeapPointerRoot, [field.FieldSymbol])
                    : PointerTo(field.Receiver, frame).Extend(field.FieldSymbol),

            Place.Deref { Reference: var ptr } => (Pointer)ReadPlace(ptr, frame),
            Place.Discard => throw new InvalidOperationException("Cannot make pointer to discard."),
            Place.Error => throw new UnreachableException(),
        };


    /// <summary>
    /// Returns the value of the given <paramref name="place"/>. Throws
    /// <see cref="InvalidOperationException"/>, if it has not been assigned to.
    /// </summary>
    private object ReadPlace(Place place, Frame frame)
        => GetPlaceRef(place, frame) ?? throw new InvalidOperationException("Place has not been assigned to yet.");

    private object Call(MutMirFunction mirFun, ImmutableArray<object> arguments)
    {
        using var frame = new Frame();
        for (var i = 0; i < mirFun.ParameterPlaces.Length; i++)
            frame.GetRef(mirFun.ParameterPlaces[i]) = arguments[i];
        
        var block = mirFun.Entry;
        while (true)
        {
            foreach (var instruction in block.Instructions)
                Execute(instruction, frame);

            var prevBlockId = block.Id;
            switch (block.Terminator)
            {
                case Terminator.Goto @goto:
                    block = @goto.Block;
                    break;

                case Terminator.Branch branch:
                    var condition = Evaluate(branch.Condition, frame);
                    block = (bool)condition ? branch.True : branch.False;
                    break;

                case Terminator.Return:
                    return ReadPlace(mirFun.ReturnPlace, frame);

                default:
                    throw new UnreachableException($"Unknown terminator {block.Terminator?.GetType().Name ?? "null"}");
            }
            
            // This is a rough estimate of backward edges. It overcounts, because it depends
            // on instantiation order rather than a pass. But this still protects infinite
            // cycles, provide some "linear code" freedom and is a quick check without another
            // pass on Mir.
            if (block.Id.Value <= prevBlockId.Value)
                UseFuel();
        }
    }

    private void Execute(Instruction instruction, Frame frame)
    {
        switch (instruction)
        {
            case Instruction.Assign assign:
            {
                var value = Evaluate(assign.Value, frame);
                GetPlaceRef(assign.Target, frame) = value;
                break;
            }
            
            case Instruction.Call call:
            {
                UseFuel();
                
                var mirFun = _program.GetMirFunction(call.Fun);
                var value = Call(mirFun,
                [
                    .. call.Arguments.Select(a => Evaluate(a, frame))
                ]);
                GetPlaceRef(call.Target, frame) = value;
                break;
            }
            
            case Instruction.Operator intrinsic:
            {
                var value = EvaluateOperator(intrinsic.Kind,
                [
                    .. intrinsic.Arguments.Select(a => Evaluate(a, frame))
                ]);
                GetPlaceRef(intrinsic.Target, frame) = value;
                break;
            }
            
            case Instruction.StructInit structInit:
            {
                var value = new StructValue(structInit.Struct);
                foreach (var kvp in structInit.FieldValues)
                    value.Fields.Add(kvp.Key, Evaluate(kvp.Value, frame));
                GetPlaceRef(structInit.Target, frame) = value;
                break;
            }
            
            case Instruction.ClassInit classInit:
            {
                var value = new ClassValue(classInit.Class);
                foreach (var kvp in classInit.FieldValues)
                    value.Fields.Add(kvp.Key, Evaluate(kvp.Value, frame));
                
                GetPlaceRef(classInit.Target, frame) = AllocateClass(value);
                break;
            }
            
            case Instruction.VmCall vmCall:
            {
                var value = EvaluateVmCall(vmCall.Kind,
                [
                    .. vmCall.Arguments.Select(a => Evaluate(a, frame))
                ]);
                GetPlaceRef(vmCall.Target, frame) = value;
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

            case VmCallKind.UnsafeBufferNewUninitialized:
            {
                var buffer = new BufferValue(new object[(int)args[0]]);
                return AllocateClass(buffer);
            }

            case VmCallKind.UnsafeBufferAtUnchecked:
                return new Pointer(new BufferPointerRoot(((ClassHandle)args[0]).HeapPointerRoot, (int)args[1]), []);
            
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

    private object Evaluate(Operand operand, Frame frame) => operand switch
    {
        Operand.Const @const => ToMiriValue(@const.Value),
        Operand.ConstEval constEval => EvaluateConstEval(constEval),
        Operand.Copy copy => Copy(ReadPlace(copy.Place, frame)),
        Operand.Ref @ref => PointerTo(@ref.Place, frame),
        Operand.SizeOf => 0,    // Miri does not need or know sizes
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