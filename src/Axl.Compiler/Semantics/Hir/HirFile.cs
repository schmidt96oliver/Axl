using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirFile(HirBody body, ImmutableArray<Diagnostic> diagnostics)
{
    public HirBody Body { get; } = body;
    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
}