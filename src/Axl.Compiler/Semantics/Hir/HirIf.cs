using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirIf(HirExpr predicate, HirExpr then, HirExpr? @else, AxlType type, SyntaxNode syntax)
    : HirExpr(type, syntax)
{
    public HirExpr Predicate { get; } = predicate;
    public HirExpr Then { get; } = then;
    public HirExpr? Else { get; } = @else;
}