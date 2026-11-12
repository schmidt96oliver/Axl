using System.Collections.Immutable;
using AxlCompiler.Diagnostics;
using AxlCompiler.Symbols;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundScript(FunSymbol scriptFun, ImmutableArray<Diagnostic> diagnostics)
{
    public FunSymbol ScriptFun { get; } = scriptFun;

    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
}