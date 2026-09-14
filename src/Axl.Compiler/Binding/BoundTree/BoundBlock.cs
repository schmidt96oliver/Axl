using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundBlock(
    ImmutableArray<BoundStmt> stmts,
    TypeSymbol type,
    SyntaxNode syntax) : BoundExpr(type, syntax)
{
    public ImmutableArray<BoundStmt> Stmts { get; } = stmts;

    protected override ImmutableArray<BoundStmt> GetChildren()
        => Stmts;

}