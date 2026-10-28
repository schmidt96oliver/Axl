using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundBlock(
    ImmutableArray<BoundStmt> stmts,
    ImmutableArray<LocalFunSymbol> localFuns,
    TypeSymbol type,
    SyntaxNode? syntax = null) : BoundValue(type, syntax)
{
    public ImmutableArray<BoundStmt> Stmts { get; } = stmts;
    public ImmutableArray<LocalFunSymbol> LocalFuns { get; } = localFuns;

    public override IEnumerable<BoundStmt> GetChildren()
        => Stmts;

    public IEnumerable<FunSymbol> LocalFunsRecursive()
    {
        return Inner(this);

        IEnumerable<FunSymbol> Inner(BoundStmt stmt) => stmt is BoundBlock block
            ?
            [
                .. block.LocalFuns, .. stmt.GetChildren().SelectMany(Inner)
            ]
            : stmt.GetChildren().SelectMany(Inner);
    }
}