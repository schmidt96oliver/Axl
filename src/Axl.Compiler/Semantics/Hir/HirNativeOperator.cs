using System.Collections.Immutable;
using Axl.Compiler.Semantics.Types;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirNativeOperator(NativeOperatorInfo operatorInfo, ImmutableArray<HirExpr> operands, AxlType type)
    : HirExpr(type)
{
    public NativeOperatorInfo OperatorInfo { get; } = operatorInfo;
    public ImmutableArray<HirExpr> Operands { get; } = operands;
}