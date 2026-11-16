using AxlCompiler.Binding;
using AxlCompiler.Symbols;

namespace AxlCompiler.Mir.Interpreter;

public sealed record MiriContext(
    TypeSymbol UnitType,
    TextWriter StdOut,
    TextWriter StdErr,
    int? BranchLimit,
    Func<ConstSymbol, ConstValue?> TryGetConstEvaluation);