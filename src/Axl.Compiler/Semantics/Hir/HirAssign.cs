using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirAssign(LocalSymbol target, HirExpr value, AxlType type, SyntaxNode syntax)
    : HirExpr(type, syntax)
{
    public LocalSymbol Target { get; } = target;
    public HirExpr Value { get; } = value;
}