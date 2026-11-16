using System.Collections.Immutable;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundBlock(
    ImmutableArray<BoundStmt> stmts,
    TypeSymbol type,
    SyntaxNode? syntax = null) : BoundValue(syntax)
{
    public ImmutableArray<BoundStmt> Stmts { get; } = stmts;

    /// <inheritdoc />
    public override TypeSymbol Type { get; } = type;

    public override IEnumerable<BoundStmt> GetChildren()
        => Stmts;
}