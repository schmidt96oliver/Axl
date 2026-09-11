using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirNativeOperator(
    NativeOperatorInfo operatorInfo,
    ImmutableArray<HirExpr> operands,
    TypeSymbol type,
    SyntaxNode syntax)
    : HirExpr(type, syntax)
{
    public NativeOperatorInfo OperatorInfo { get; } = operatorInfo;
    public ImmutableArray<HirExpr> Operands { get; } = operands;

    protected override ImmutableArray<HirStmt> GetChildren() => Operands.CastArray<HirStmt>();

}