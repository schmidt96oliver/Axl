using System.Collections.Immutable;
using AxlCompiler.Diagnostics;
using AxlCompiler.Mir;

namespace AxlCompiler;

public sealed record CompilationResult(MirProgram? Program, ImmutableArray<Diagnostic> Diagnostics)
{
    public bool IsSuccessful => Program is not null;
}