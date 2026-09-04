using Axl.Compiler.Semantics.Types;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirBoolLiteral(bool value, AxlType type) : HirExpr(type)
{
    public bool Value { get; } = value;
}