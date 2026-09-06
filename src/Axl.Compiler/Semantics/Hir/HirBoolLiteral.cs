using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirBoolLiteral(bool value, AxlType type, SyntaxNode syntax) : HirExpr(type, syntax)
{
    public bool Value { get; } = value;
}