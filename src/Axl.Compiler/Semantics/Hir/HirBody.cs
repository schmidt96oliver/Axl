using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

/// <summary>
/// Represents a body of code. Can be a block or an arm.
/// </summary>
public sealed class HirBody(
    ImmutableArray<HirStmt> stmts,
    HirExpr? armExpr,
    AxlType type,
    SyntaxNode syntax) : HirExpr(type, syntax)
{
    public ImmutableArray<HirStmt> Stmts { get; } = stmts;
    public HirExpr? ArmExpr { get; } = armExpr;
}