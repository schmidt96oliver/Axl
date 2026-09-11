using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public abstract class BoundExpr(TypeSymbol type, SyntaxNode syntax) : BoundStmt(syntax)
{
    public TypeSymbol Type { get; } = type;
}