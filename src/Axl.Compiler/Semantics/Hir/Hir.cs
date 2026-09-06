using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Semantics.Symbols;

namespace Axl.Compiler.Semantics.Hir;

/// <summary>
/// Represents an entire entity (script or fn body) of executable code.
/// </summary>
public sealed class Hir(HirBody body, ImmutableArray<Symbol> localMembers, ImmutableArray<Diagnostic> diagnostics)
{
    public HirBody Body { get; } = body;
    public ImmutableArray<Symbol> LocalMembers { get; } = localMembers;
    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
}