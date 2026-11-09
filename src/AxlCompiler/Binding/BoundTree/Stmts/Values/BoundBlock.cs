using System.Collections.Immutable;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundBlock(
    ImmutableArray<BoundStmt> stmts,
    TypeSymbol type,
    SyntaxNode? syntax = null) : BoundValue(type, syntax)
{
    public ImmutableArray<BoundStmt> Stmts { get; } = stmts;

    public override IEnumerable<BoundStmt> GetChildren()
        => Stmts;
}