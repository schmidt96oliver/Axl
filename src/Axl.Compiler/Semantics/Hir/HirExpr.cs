using Axl.Compiler.Semantics.Types;

namespace Axl.Compiler.Semantics.Hir;

public abstract class HirExpr(AxlType type) : HirStmt
{
    public AxlType Type { get; } = type;
}