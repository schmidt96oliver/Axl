using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundNativeOperator(
    NativeOperatorInfo operatorInfo,
    ImmutableArray<BoundExpr> operands,
    TypeSymbol type,
    SyntaxNode syntax)
    : BoundExpr(type, syntax)
{
    public NativeOperatorInfo OperatorInfo { get; } = operatorInfo;
    public ImmutableArray<BoundExpr> Operands { get; } = operands;

    protected override ImmutableArray<BoundStmt> GetChildren() => Operands.CastArray<BoundStmt>();

}