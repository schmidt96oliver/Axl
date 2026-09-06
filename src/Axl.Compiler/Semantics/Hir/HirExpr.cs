using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public abstract class HirExpr(AxlType type, SyntaxNode syntax) : HirStmt(syntax)
{
    public AxlType Type { get; } = type;
}