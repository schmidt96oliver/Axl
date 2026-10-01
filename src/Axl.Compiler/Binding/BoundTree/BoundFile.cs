using System.Collections.Frozen;
using System.Collections.Immutable;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Symbols;
using Axl.Compiler.Text;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundFile(FunSymbol scriptFun, ImmutableArray<Diagnostic> diagnostics, FrozenDictionary<SourceLocation, Symbol> resolvedSymbols)
{
    public FunSymbol ScriptFun { get; } = scriptFun;
    
    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
    
    public FrozenDictionary<SourceLocation, Symbol> ResolvedSymbols { get; } = resolvedSymbols;

    public Symbol? TryGetSymbol(SourceLocation location)
        => ResolvedSymbols.GetValueOrDefault(location);
}