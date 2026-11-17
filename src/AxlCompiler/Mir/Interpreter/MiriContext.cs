using AxlCompiler.Binding;
using AxlCompiler.Symbols;

namespace AxlCompiler.Mir.Interpreter;

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
    int? BranchLimit,
    Func<ConstSymbol, ConstValue?>? TryGetConstEvaluation);