using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Mir;

namespace Axl.Compiler;

public sealed record CompilationResult(MirProgram? Program, ImmutableArray<Diagnostic> Diagnostics)
{
    public bool IsSuccessful => Program is not null;
}