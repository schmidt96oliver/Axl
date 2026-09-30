using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public closed class BoundExpr(TypeSymbol type, SyntaxNode? syntax = null) : BoundStmt(syntax)
{
    public TypeSymbol Type { get; } = type;
}