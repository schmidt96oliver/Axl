using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;

namespace Axl.Compiler;

public sealed class BoundTreeInterpreter
{
    private sealed class Environment(object? receiver) : Dictionary<VariableSymbol, object>()
    {
        public object? Receiver = receiver;
    }
    
    private sealed class BreakException : Exception;
    private sealed class ContinueException : Exception;

    private sealed class ReturnException(object value) : Exception
    {
        public object Value { get; } = value;
    }

    private sealed record StructValue(Dictionary<FieldSymbol, object> FieldValues);


    private readonly TextWriter _output;
    private Environment _env = new(null);
    private readonly object _emptyStruct = new StructValue(FieldValues: []);

    private BoundTreeInterpreter(TextWriter output)
    {
        _output = output;
    }
    
    
    public static void Run(FunSymbol script, TextWriter output)
    {
        var interpreter = new BoundTreeInterpreter(output);

        try
        {
            interpreter.Call(script, null, []);
        }
        catch (ReturnException) {}
    }

    private object Call(FunSymbol fun, object? receiver, ImmutableArray<object> args)
    {
        if (fun.Body is Intrinsic intrinsic)
            return CallIntrinsic(intrinsic, receiver, args);
        if (fun.Body is not BoundBlock block)
            throw new UnreachableException();

        var prevEnvironment = _env;
        _env = new Environment(receiver);

        for (var i = 0; i < fun.Parameters.Length; i++)
            _env[fun.Parameters[i]] = args[i];

        try
        {
            Run(block);
        }
        catch (ReturnException returnException)
        {
            _env = prevEnvironment;
            return returnException.Value;
        }

        // For all fun bodies, the Binder ensures that all code paths return.
        // Otherwise, it errors or synthesizes a return.
        throw new UnreachableException("Not all code paths return a value.");
    }

    private object CallIntrinsic(Intrinsic intrinsic, object? receiver, ImmutableArray<object> args)
    {
        if (intrinsic is Intrinsic.Print)
        {
            _output.Write(args[0].ToString());
            return _emptyStruct;
        }
        
        return intrinsic switch
            {
                Intrinsic.NegateI32 => -(int)receiver!,
                Intrinsic.AddI32 => (int)receiver! + (int)args[0],
                Intrinsic.SubtractI32 => (int)receiver! - (int)args[0],
                Intrinsic.DivideI32 => (int)receiver! / (int)args[0],
                Intrinsic.MultiplyI32 => (int)receiver! * (int)args[0],
                Intrinsic.EqualsI32 => (int)receiver! == (int)args[0],
                Intrinsic.LessThanI32 => (int)receiver! < (int)args[0],

                Intrinsic.NegateI64 => -(long)receiver!,
                Intrinsic.AddI64 => (long)receiver! + (long)args[0],
                Intrinsic.SubtractI64 => (long)receiver! - (long)args[0],
                Intrinsic.DivideI64 => (long)receiver! / (long)args[0],
                Intrinsic.MultiplyI64 => (long)receiver! * (long)args[0],
                Intrinsic.EqualsI64 => (long)receiver! == (long)args[0],
                Intrinsic.LessThanI64 => (long)receiver! < (long)args[0],

                Intrinsic.NegateF32 => -(float)receiver!,
                Intrinsic.AddF32 => (float)receiver! + (float)args[0],
                Intrinsic.SubtractF32 => (float)receiver! - (float)args[0],
                Intrinsic.DivideF32 => (float)receiver! / (float)args[0],
                Intrinsic.MultiplyF32 => (float)receiver! * (float)args[0],
                Intrinsic.EqualsF32 => (float)receiver! == (float)args[0],
                Intrinsic.LessThanF32 => (float)receiver! < (float)args[0],

                Intrinsic.NegateF64 => -(double)receiver!,
                Intrinsic.AddF64 => (double)receiver! + (double)args[0],
                Intrinsic.SubtractF64 => (double)receiver! - (double)args[0],
                Intrinsic.DivideF64 => (double)receiver! / (double)args[0],
                Intrinsic.MultiplyF64 => (double)receiver! * (double)args[0],
                Intrinsic.EqualsF64 => (double)receiver! == (double)args[0],
                Intrinsic.LessThanF64 => (double)receiver! < (double)args[0],

                Intrinsic.NotBool => !(bool)receiver!,
                Intrinsic.EqualsBool => (bool)receiver! == (bool)args[0],

                Intrinsic.EqualsString => (string)receiver! == (string)args[0],
                Intrinsic.ConcatString => string.Concat((string)receiver!, (string)args[0]),

                Intrinsic.ToStringI32 or Intrinsic.ToStringI64 => receiver!.ToString()!,
                Intrinsic.ToStringBool => (bool)receiver! ? "true" : "false",
                Intrinsic.ToStringF32 => ((float)receiver!).ToString(CultureInfo.InvariantCulture)!,
                Intrinsic.ToStringF64 => ((double)receiver!).ToString(CultureInfo.InvariantCulture)!,

                _ => throw new UnreachableException()
            };
    }


    private void Run(BoundStmt stmt)
    {
        switch (stmt)
        {
            case BoundWhile boundWhile:
                RunWhile(boundWhile);
                break;
            
            case BoundIfStmt boundIfStmt:
                RunIf(boundIfStmt);
                break;
            case BoundVarDecl boundVarDecl:
                _env[boundVarDecl.Variable] = Evaluate(boundVarDecl.Initializer);
                break;
            
            case BoundValue boundExpr:
                Evaluate(boundExpr);
                break;
        }
    }

    private void RunIf(BoundIfStmt boundIfStmt)
    {
        var condition = (bool)Evaluate(boundIfStmt.Condition);
        if (condition) Run(boundIfStmt.Then);
        else if (boundIfStmt.Else is not null) Run(boundIfStmt.Else);
    }

    private void RunWhile(BoundWhile boundWhile)
    {
        while (true)
        {
            var condition = (bool)Evaluate(boundWhile.Condition);
            if (!condition) break;

            try
            {
                Run(boundWhile.Body);
            }
            catch (BreakException)
            {
                break;
            }
            catch (ContinueException)
            {
                continue;
            }
        }
    }

    private object Evaluate(BoundValue value) => value switch
    {
        BoundValueOrTypeRef valueOrType => Evaluate(valueOrType.AsValue),

        BoundSelf boundSelf => CopyOrAlias(GetPlaceRef(boundSelf)),
        BoundVariable boundVariableRef => CopyOrAlias(GetPlaceRef(boundVariableRef)),
        BoundConst boundConst => boundConst.Value.Value!,

        BoundLogicAnd boundAnd => EvaluateAnd(boundAnd),
        BoundAssign boundAssign => EvaluateAssign(boundAssign),
        BoundBlock boundBlock => EvaluateBlock(boundBlock),
        BoundCall boundCall => EvaluateCall(boundCall),
        BoundIfExpr boundIfExpr => EvaluateIf(boundIfExpr),

        BoundLogicOr boundOr => EvaluateOr(boundOr),

        BoundBreak => throw new BreakException(),
        BoundContinue => throw new ContinueException(),
        BoundReturn boundReturn => EvaluateReturn(boundReturn),

        BoundStructInit structInit => EvaluateStructInit(structInit),
        BoundField field => EvaluateField(field),

        BoundFieldInit => throw new UnreachableException(),
        BoundError => throw new InvalidOperationException("Cannot evaluate errors."),
    };

    private object EvaluateField(BoundField field)
    {
        // When just accessing fields, we don't need to copy the receiver.
        var fieldRef = field.IsPlace
            ? GetPlaceRef(field)
            : ((StructValue)Evaluate(field.Receiver)).FieldValues[field.Field];
        
        return CopyOrAlias(fieldRef);
    }

    private ref object GetPlaceRef(BoundValue placeValue)
    {
        switch (placeValue)
        {
            case BoundVariable boundVar:
                return ref CollectionsMarshal.GetValueRefOrNullRef(_env, boundVar.Variable);
            case BoundField boundField:
                var receiver = (StructValue)GetPlaceRef(boundField.Receiver);
                return ref CollectionsMarshal.GetValueRefOrNullRef(receiver.FieldValues, boundField.Field);
            case BoundSelf:
                return ref _env.Receiver!;
            default:
                throw new UnreachableException("Given value is not a place.");
        }
    }

    private object CopyOrAlias(object value)
    {
        if (value is StructValue structValue)
        {
            // If it has no fields, there is nothing to copy.
            if (structValue.FieldValues.Count == 0)
                return structValue;
            
            var fieldValues = new Dictionary<FieldSymbol, object>();
            foreach (var originalValueByField in structValue.FieldValues)
            {
                fieldValues.Add(originalValueByField.Key,
                    CopyOrAlias(originalValueByField.Value));
            }

            return new StructValue(fieldValues);
        }

        return value;
    }
    
    private object EvaluateStructInit(BoundStructInit structInit)
    {
        if (structInit.FieldInits.Length == 0)
            return _emptyStruct;
        
        var fieldValues = new Dictionary<FieldSymbol, object>();
        foreach (var fieldInit in structInit.FieldInits)
            fieldValues[fieldInit.Field] = Evaluate(fieldInit.Value);

        return new StructValue(fieldValues);
    }

    private object EvaluateReturn(BoundReturn boundReturn)
    {
        var value = boundReturn.Value is not null
            ? Evaluate(boundReturn.Value)
            : _emptyStruct;
        
        throw new ReturnException(value);
    }

    private object EvaluateAssign(BoundAssign boundAssign)
    {
        GetPlaceRef(boundAssign.Target) = Evaluate(boundAssign.Value);
        return _emptyStruct;
    }

    private object EvaluateBlock(BoundBlock boundBlock)
    {
        foreach (var stmt in boundBlock.Stmts)
            Run(stmt);
        return _emptyStruct;
    }

    private object EvaluateOr(BoundLogicOr boundLogicOr)
    {
        var left = (bool)Evaluate(boundLogicOr.Left);
        if (left) return true;
        return Evaluate(boundLogicOr.Right);
    }
    
    private object EvaluateAnd(BoundLogicAnd boundLogicAnd)
    {
        var left = (bool)Evaluate(boundLogicAnd.Left);
        if (!left) return false;
        return Evaluate(boundLogicAnd.Right);
    }

    private object EvaluateIf(BoundIfExpr boundIfExpr)
    {
        var condition = (bool)Evaluate(boundIfExpr.Condition);
        if (condition) return Evaluate(boundIfExpr.Then);
        else return Evaluate(boundIfExpr.Else);
    }

    
    private object EvaluateCall(BoundCall boundCall)
    {
        var receiver = boundCall.Receiver is not null
            ? boundCall.Receiver.IsPlace
                ? GetPlaceRef(boundCall.Receiver)
                : Evaluate(boundCall.Receiver)
            : null;
        var args = boundCall.Arguments.Select(Evaluate).ToImmutableArray();
        
        return Call(boundCall.Fun, receiver, args);
    }
}