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
    private sealed class BreakException : Exception;
    private sealed class ContinueException : Exception;

    private sealed class ReturnException : Exception;
    
    
    private readonly TextWriter _output;
    private readonly Dictionary<VariableSymbol, object> _values = [];
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
            interpreter.Run(file.Block);
        }
        catch (ReturnException) {}
    }

    private static void PrintDiagnostics(TextWriter output, ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var diag in diagnostics)
            output.WriteLine($"[{diag.DefaultSeverity.ToString().ToUpper()}] {diag.Id}@l.{diag.Locations[0].StartLine}: {diag.Message}");
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
                _values[boundVarDecl.Variable] = Evaluate(boundVarDecl.Initializer);
                break;
            
            case BoundExpr boundExpr:
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

    private object Evaluate(BoundExpr expr) => expr switch
    {
        BoundVariableRef boundVariableRef => _values[boundVariableRef.Variable],
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
        BoundReturn => throw new ReturnException(),
        
        BoundErrorExpr => throw new UnreachableException(),
    };

    private object EvaluateAssign(BoundAssign boundAssign)
    {
        _values[boundAssign.Target] = Evaluate(boundAssign.Value);
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
        var args = new object[boundCall.Arguments.Length];
        for (var i = 0; i < args.Length; i++)
        {
            var val = Evaluate(boundCall.Arguments[i]);
            args[i] = val;
        }
        // var args = boundCall.Arguments.Select(Evaluate).ToArray();

        if (boundCall.Fun.Intrinsic is Intrinsic.Print)
        {
            _output.Write(args[0].ToString());
            return _unitValue;
        }

        return boundCall.Fun.Intrinsic switch
        {
            Intrinsic.NegateI32 => -(int)args[0],
            Intrinsic.AddI32 => (int)args[0] + (int)args[1],
            Intrinsic.SubtractI32 => (int)args[0] - (int)args[1],
            Intrinsic.DivideI32 => (int)args[0] / (int)args[1],
            Intrinsic.MultiplyI32 => (int)args[0] * (int)args[1],
            Intrinsic.EqualsI32 => (int)args[0] == (int)args[1],
            Intrinsic.NotEqualsI32 => (int)args[0] != (int)args[1],
            Intrinsic.LessThanI32 => (int)args[0] < (int)args[1],
            Intrinsic.LessThanOrEqualI32 => (int)args[0] <= (int)args[1],
            Intrinsic.GreaterThanI32 => (int)args[0] > (int)args[1],
            Intrinsic.GreaterThanOrEqualI32 => (int)args[0] >= (int)args[1],

            Intrinsic.NegateI64 => -(long)args[0],
            Intrinsic.AddI64 => (long)args[0] + (long)args[1],
            Intrinsic.SubtractI64 => (long)args[0] - (long)args[1],
            Intrinsic.DivideI64 => (long)args[0] / (long)args[1],
            Intrinsic.MultiplyI64 => (long)args[0] * (long)args[1],
            Intrinsic.EqualsI64 => (long)args[0] == (long)args[1],
            Intrinsic.NotEqualsI64 => (long)args[0] != (long)args[1],
            Intrinsic.LessThanI64 => (long)args[0] < (long)args[1],
            Intrinsic.LessThanOrEqualI64 => (long)args[0] <= (long)args[1],
            Intrinsic.GreaterThanI64 => (long)args[0] > (long)args[1],
            Intrinsic.GreaterThanOrEqualI64 => (long)args[0] >= (long)args[1],

            Intrinsic.NegateF32 => -(float)args[0],
            Intrinsic.AddF32 => (float)args[0] + (float)args[1],
            Intrinsic.SubtractF32 => (float)args[0] - (float)args[1],
            Intrinsic.DivideF32 => (float)args[0] / (float)args[1],
            Intrinsic.MultiplyF32 => (float)args[0] * (float)args[1],
            Intrinsic.EqualsF32 => (float)args[0] == (float)args[1],
            Intrinsic.NotEqualsF32 => (float)args[0] != (float)args[1],
            Intrinsic.LessThanF32 => (float)args[0] < (float)args[1],
            Intrinsic.LessThanOrEqualF32 => (float)args[0] <= (float)args[1],
            Intrinsic.GreaterThanF32 => (float)args[0] > (float)args[1],
            Intrinsic.GreaterThanOrEqualF32 => (float)args[0] >= (float)args[1],

            Intrinsic.NegateF64 => -(double)args[0],
            Intrinsic.AddF64 => (double)args[0] + (double)args[1],
            Intrinsic.SubtractF64 => (double)args[0] - (double)args[1],
            Intrinsic.DivideF64 => (double)args[0] / (double)args[1],
            Intrinsic.MultiplyF64 => (double)args[0] * (double)args[1],
            Intrinsic.EqualsF64 => (double)args[0] == (double)args[1],
            Intrinsic.NotEqualsF64 => (double)args[0] != (double)args[1],
            Intrinsic.LessThanF64 => (double)args[0] < (double)args[1],
            Intrinsic.LessThanOrEqualF64 => (double)args[0] <= (double)args[1],
            Intrinsic.GreaterThanF64 => (double)args[0] > (double)args[1],
            Intrinsic.GreaterThanOrEqualF64 => (double)args[0] >= (double)args[1],

            Intrinsic.NotBool => !(bool)args[0],
            Intrinsic.EqualsBool => (bool)args[0] == (bool)args[1],
            Intrinsic.NotEqualsBool => (bool)args[0] != (bool)args[1],

            Intrinsic.EqualsUnit => true,
            Intrinsic.NotEqualsUnit => false,

            Intrinsic.EqualsString => (string)args[0] == (string)args[1],
            Intrinsic.NotEqualsString => (string)args[0] != (string)args[1],

            Intrinsic.ToStringI32 or Intrinsic.ToStringI64
                or Intrinsic.ToStringBool => args[0].ToString(),
            Intrinsic.ToStringF32 => ((float)args[0]).ToString(CultureInfo.InvariantCulture),
            Intrinsic.ToStringF64 => ((double)args[0]).ToString(CultureInfo.InvariantCulture),

            _ => throw new UnreachableException()
        };
    }
}