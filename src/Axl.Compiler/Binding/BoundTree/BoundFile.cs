using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundFile(BoundBody body, ImmutableArray<Diagnostic> diagnostics)
{
    public BoundBody Body { get; } = body;
    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
}