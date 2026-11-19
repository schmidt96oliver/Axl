using System.Collections.Immutable;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public class BoundTypeInitError(TypeSymbol symbol, ImmutableArray<BoundArgument> boundArguments, ErrorTypeSymbol type, SyntaxNode? syntax = null)
    : BoundError(type, syntax)
{
    public TypeSymbol Symbol { get; } = symbol;
    public ImmutableArray<BoundArgument> BoundArguments { get; } = boundArguments;

    public override IEnumerable<BoundStmt> GetChildren()
        => [.. BoundArguments.Select(arg => arg.Value)];
}