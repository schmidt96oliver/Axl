using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Symbols;

namespace Axl.Compiler.Semantics.Hir;

/// <summary>
/// Represents an entire entity (script or fn body) of executable code.
/// </summary>
public sealed class Hir(HirBody body, ImmutableArray<FnSymbol> localFns, ImmutableArray<Diagnostic> diagnostics)
{
    public HirBody Body { get; } = body;
    public ImmutableArray<FnSymbol> LocalFns { get; } = localFns;
    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
}