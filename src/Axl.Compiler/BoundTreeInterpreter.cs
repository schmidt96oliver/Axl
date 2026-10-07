using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Axl.Compiler.Binding.BoundTree;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;

namespace Axl.Compiler;

public sealed class BoundTreeInterpreter
{
    private sealed class Environment(object? receiver) : Dictionary<VariableSymbol, object>()
    {
        public object? Receiver { get; } = receiver;
    }
    
    private sealed class BreakException : Exception;
    private sealed class ContinueException : Exception;

    private sealed class ReturnException(object value) : Exception
    {
        public object Value { get; } = value;
    }


    private readonly TextWriter _output;
    private Environment _env = new(null);
    private readonly object _unitValue = new();

    private BoundTreeInterpreter(TextWriter output)
    {
        _output = output;
    }
    
    
    public static void Run(BoundFile file, TextWriter output)
    {
        PrintDiagnostics(output, file.Diagnostics);
        if (file.Diagnostics.Any(diag => diag is Diagnostic.Error))
            return;
        
        var interpreter = new BoundTreeInterpreter(output);

        try
        {
            interpreter.Call(file.ScriptFun, null, []);
        }
        catch (ReturnException) {}
    }

    private static void PrintDiagnostics(TextWriter output, ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var diag in diagnostics)
            output.WriteLine($"[{diag.DefaultSeverity.ToString().ToUpper()}] {diag.Id}@l.{diag.Locations[0].StartLine}: {diag.Message}");
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
            return _unitValue;
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

                Intrinsic.EqualsUnit => true,
                Intrinsic.EqualsString => (string)receiver! == (string)args[0],

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
        BoundSelf => _env.Receiver ?? throw new UnreachableException(),
        BoundVariable boundVariableRef => _env[boundVariableRef.Variable],
        BoundConst boundConst => boundConst.Value.Value!,
        
        BoundAnd boundAnd => EvaluateAnd(boundAnd),
        BoundAssign boundAssign => EvaluateAssign(boundAssign),
        BoundBlock boundBlock => EvaluateBlock(boundBlock),
        BoundCall boundCall => EvaluateCall(boundCall),
        BoundIfExpr boundIfExpr => EvaluateIf(boundIfExpr),
        
        BoundOr boundOr => EvaluateOr(boundOr),
        BoundStringExpr boundStringExpr => string.Concat(boundStringExpr.Parts.Select(part => (string)Evaluate(part))),
        
        BoundBreak => throw new BreakException(),
        BoundContinue => throw new ContinueException(),
        BoundReturn boundReturn => EvaluateReturn(boundReturn),
        
        BoundFieldInit or BoundStructInit or BoundFieldAccess => throw new NotImplementedException(),
        
        BoundError => throw new UnreachableException(),
    };

    private object EvaluateReturn(BoundReturn boundReturn)
    {
        var value = boundReturn.Value is not null
            ? Evaluate(boundReturn.Value)
            : _unitValue;
        
        throw new ReturnException(value);
    }

    private object EvaluateAssign(BoundAssign boundAssign)
    {
        if (boundAssign.Target is not BoundVariable boundVar) throw new UnreachableException();
        
        _env[boundVar.Variable] = Evaluate(boundAssign.Value);
        return _unitValue;
    }

    private object EvaluateBlock(BoundBlock boundBlock)
    {
        foreach (var stmt in boundBlock.Stmts)
            Run(stmt);
        return _unitValue;
    }

    private object EvaluateOr(BoundOr boundOr)
    {
        var left = (bool)Evaluate(boundOr.Left);
        if (left) return true;
        return Evaluate(boundOr.Right);
    }
    
    private object EvaluateAnd(BoundAnd boundAnd)
    {
        var left = (bool)Evaluate(boundAnd.Left);
        if (!left) return false;
        return Evaluate(boundAnd.Right);
    }

    private object EvaluateIf(BoundIfExpr boundIfExpr)
    {
        var condition = (bool)Evaluate(boundIfExpr.Condition);
        if (condition) return Evaluate(boundIfExpr.Then);
        else return Evaluate(boundIfExpr.Else);
    }

    
    private object EvaluateCall(BoundCall boundCall)
    {
        var receiver = boundCall.Receiver is not null ? Evaluate(boundCall.Receiver) : null;
        var args = boundCall.Arguments.Select(Evaluate).ToImmutableArray();
        
        return Call(boundCall.Fun, receiver, args);
    }
}