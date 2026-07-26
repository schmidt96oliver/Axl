
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Axl.Compiler;

internal static class Guard
{
    [DebuggerHidden]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InRange<T>(T arg, 
        bool condition, 
        [CallerArgumentExpression(nameof(arg))] string? paramName = null,
        [CallerArgumentExpression(nameof(condition))] string? expected = null)
        where T: INumber<T>
    {
        if (!condition)
            throw new ArgumentOutOfRangeException(paramName, arg, $"{paramName} was out of range, expected {expected}.");
    }
}