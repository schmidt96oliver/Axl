using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Axl.Compiler;

public sealed class CompilerCycleException(string message) : Exception(message);

/// <summary>
/// A field that is lazily computed and guards against cyclic calls
/// on being computed.
/// </summary>
public struct LazyField<T>
{
    private enum State : byte
    {
        NotStarted,
        InProgress,
        Done
    }
    
    private T? _value;
    private State _state;


    [DebuggerHidden]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOrCreate<TArg>(TArg arg, Func<TArg, T> factory,
        [CallerMemberName] string? memberName = null)
    {
        if (_state is State.Done)
            return _value!;

        if (_state is State.InProgress) 
            ThrowCycle(memberName);

        _state = State.InProgress;
        _value = factory(arg);
        _state = State.Done;
        
        return _value;
    }

    [DebuggerHidden]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOrCreate(Func<T> factory,
        [CallerMemberName] string? memberName = null)
    {
        if (_state is State.Done)
            return _value!;

        if (_state is State.InProgress) 
            ThrowCycle(memberName);

        _state = State.InProgress;
        _value = factory();
        _state = State.Done;
        
        return _value;
    }

    [DebuggerHidden]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowCycle(string? memberName)
        => throw new CompilerCycleException($"Cycle evaluating '{memberName}'.");
}