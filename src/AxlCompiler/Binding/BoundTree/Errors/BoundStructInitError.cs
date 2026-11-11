using System.Collections.Immutable;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public class BoundStructInitError(ImmutableArray<TypeSymbol> candidates, ImmutableArray<BoundArgument> boundArguments, ErrorTypeSymbol type, SyntaxNode? syntax = null)
    : BoundError(type, syntax)
{
    public ImmutableArray<TypeSymbol> Candidates { get; } = candidates;
    public ImmutableArray<BoundArgument> BoundArguments { get; } = boundArguments;

    public override IEnumerable<BoundStmt> GetChildren()
        => [.. BoundArguments.Select(arg => arg.Value)];
}