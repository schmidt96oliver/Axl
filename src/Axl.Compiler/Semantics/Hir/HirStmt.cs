using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public abstract class HirStmt(SyntaxNode syntax)
{
    public SyntaxNode Syntax { get; } = syntax;
}