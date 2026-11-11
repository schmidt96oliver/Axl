using System.Collections.Immutable;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundLookupError(ImmutableArray<Symbol> candidates, UnresolvedSymbolReason reason, ErrorTypeSymbol type, SyntaxNode? syntax) 
    : BoundError(type, syntax)
{
    public ImmutableArray<Symbol> Candidates { get; } = candidates;
    public UnresolvedSymbolReason Reason { get; } = reason;
}