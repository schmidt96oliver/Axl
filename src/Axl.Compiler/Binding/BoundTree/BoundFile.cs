using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundFile(BoundBlock block, ImmutableArray<Diagnostic> diagnostics)
{
    public BoundBlock Block { get; } = block;
    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
}