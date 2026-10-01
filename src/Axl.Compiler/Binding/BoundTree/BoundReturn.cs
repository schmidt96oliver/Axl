using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundReturn(BoundExpr? expr, SyntaxNode? syntax = null)
    : BoundExpr(NeverTypeSymbol.Instance, syntax)
{
    public BoundExpr? Expr { get; } = expr;
}