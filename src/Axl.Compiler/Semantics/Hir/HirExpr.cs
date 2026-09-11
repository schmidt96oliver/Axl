using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public abstract class HirExpr(TypeSymbol type, SyntaxNode syntax) : HirStmt(syntax)
{
    public TypeSymbol Type { get; } = type;
}