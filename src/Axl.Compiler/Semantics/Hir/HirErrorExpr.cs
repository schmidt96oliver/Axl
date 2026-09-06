using System.Collections.Immutable;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public class HirErrorExpr(ImmutableArray<HirExpr> recoveredExprs, ErrorType type, SyntaxNode syntax) : HirExpr(type, syntax)
{
    public ImmutableArray<HirExpr> RecoveredExprs { get; } = recoveredExprs;
}