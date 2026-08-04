
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Axl.Compiler;

internal static class Guard
{
    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="condition"/> is <c>false</c>.
    /// </summary>
    /// <example>
    /// Guard.InRange(index >= 0);
    /// </example>
    [DebuggerHidden]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InRange(
        [DoesNotReturnIf(false)] bool condition,
        [CallerArgumentExpression(nameof(condition))] string? expression = null)
    {
        if (!condition) ThrowOutOfRange(expression);
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if <paramref name="condition"/> is <c>false</c>.
    /// </summary>
    /// <example>
    /// Guard.MustBe(token.Kind is TokenKind.Semicolon);
    /// </example>
    [DebuggerHidden]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MustBe(
        [DoesNotReturnIf(false)] bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string? expression = null)
    {
        if (!condition) ThrowArgumentMust(message, expression);
    }
    
    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> if <paramref name="condition"/> is <c>false</c>.
    /// </summary>
    /// <example>
    /// Guard.IsState(!IsUsed);
    /// </example>
    [DebuggerHidden]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IsState(
        [DoesNotReturnIf(false)] bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string? expression = null)
    {
        if (!condition) ThrowInvalidOperation(message, expression);
    }

    
    
    // ---- Cold Paths
    // These methods are the cold paths and will (probably) not get inlined, whereas the methods
    // above are slim and will get inlined for performance.
    
    [DebuggerHidden]
    [StackTraceHidden]
    [DoesNotReturn]
    private static void ThrowOutOfRange(string? expected)
        => throw new ArgumentOutOfRangeException(paramName: null, $"Argument must be: {expected}");
    
    [DebuggerHidden]
    [StackTraceHidden]
    [DoesNotReturn]
    private static void ThrowArgumentMust(string? message, string? expected)
        => throw new ArgumentException(message ?? $"Argument must be: {expected}");
    
    [DebuggerHidden]
    [StackTraceHidden]
    [DoesNotReturn]
    private static void ThrowInvalidOperation(string? message, string? expected)
        => throw new InvalidOperationException(message ?? $"Object must be in state: {expected}");
}