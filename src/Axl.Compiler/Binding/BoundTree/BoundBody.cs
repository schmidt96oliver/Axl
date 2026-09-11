using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

/// <summary>
/// Represents a body of code. Can be a block or an arm.
/// </summary>
public sealed class BoundBody(
    ImmutableArray<BoundStmt> stmts,
    BoundExpr? armExpr,
    TypeSymbol type,
    SyntaxNode syntax) : BoundExpr(type, syntax)
{
    public ImmutableArray<BoundStmt> Stmts { get; } = stmts;
    public BoundExpr? ArmExpr { get; } = armExpr;

    protected override ImmutableArray<BoundStmt> GetChildren()
        => ArmExpr is not null ? [.. Stmts, ArmExpr] : Stmts;

}