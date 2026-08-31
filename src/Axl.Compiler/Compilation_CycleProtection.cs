using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
// ReSharper disable InconsistentNaming

namespace Axl.Compiler;

public enum QueryKind
{
    Compilation_Diagnostics
}

public class CyclicQueryException(string message) : Exception(message);

public partial class Compilation
{
    private readonly record struct Query(QueryKind Kind, object? Argument)
    {
        public override string ToString()
            => $"{Kind}({Argument})";
    }
    
    
    private readonly Stack<Query> _activeQueries = [];
    
    
    [StackTraceHidden]
    [DebuggerStepThrough]
    internal TReturn ProtectCycles<TReturn>(QueryKind kind, Func<TReturn> action)
    {
        var query = new Query(kind, Argument: null);
        if (_activeQueries.Contains(query))
            ThrowCyclicQueryException(query);
        
        _activeQueries.Push(query);
        var result = action();
        
        var poppedKey = _activeQueries.Pop();
        Debug.Assert(poppedKey == query);
        return result;
    }

    [StackTraceHidden]
    [DebuggerStepThrough]
    internal TReturn ProtectCycles<TArg, TReturn>(QueryKind kind, Func<TArg, TReturn> action, TArg argument)
    {
        var query = new Query(kind, Argument: argument);
        if (_activeQueries.Contains(query))
            ThrowCyclicQueryException(query);

        _activeQueries.Push(query);
        var result = action(argument);

        var poppedKey = _activeQueries.Pop();
        Debug.Assert(poppedKey == query);
        return result;
    }


    [DoesNotReturn]
    [StackTraceHidden]
    [DebuggerStepThrough]
    private void ThrowCyclicQueryException(Query cyclicQuery)
    {
        var stack = string.Join("\n", _activeQueries);
        throw new CyclicQueryException($"Compilation queries were cyclic at query \"{cyclicQuery}\". Active queries: \n{stack}");
    }
}