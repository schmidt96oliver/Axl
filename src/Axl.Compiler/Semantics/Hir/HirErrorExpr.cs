using System.Collections.Immutable;
using Axl.Compiler.Semantics.Types;

namespace Axl.Compiler.Semantics.Hir;

public class HirErrorExpr(ImmutableArray<HirExpr> recoveredExprs, ErrorType type) : HirExpr(type)
{
    public ImmutableArray<HirExpr> RecoveredExprs { get; } = recoveredExprs;
}