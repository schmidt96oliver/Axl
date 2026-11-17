using AxlCompiler.Binding;
using AxlCompiler.Symbols;

namespace AxlCompiler.Mir.Interpreter;

/// <param name="Fuel">
/// Used to prevent Miri from hitting infinite loops. Fuel will be taken for backward
/// jumps and calls. It might still overcount.
/// </param>
/// <param name="TryGetConstEvaluation">
/// The callback for evaluation <see cref="Operand.ConstEval"/>. If it returns
/// <c>null</c> for a specific <see cref="ConstSymbol"/>, Miri will stop
/// with <see cref="MiriResult.CannotEvaluateConst"/>. If <paramref name="TryGetConstEvaluation"/>
/// is <c>null</c>, Miri will run constants like a static fun.
/// </param>
public sealed record MiriContext(
    TypeSymbol UnitType,
    TextWriter? StdOut,
    TextWriter? StdErr,
    int? Fuel,
    Func<ConstSymbol, ConstValue?>? TryGetConstEvaluation);