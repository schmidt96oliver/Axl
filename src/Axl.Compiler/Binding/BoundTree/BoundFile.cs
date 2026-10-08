using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundFile(FunSymbol scriptFun, ImmutableArray<Diagnostic> diagnostics, SemanticSideTable semanticSideTable)
{
    public FunSymbol ScriptFun { get; } = scriptFun;

    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
    public SemanticSideTable SemanticSideTable { get; } = semanticSideTable;
}