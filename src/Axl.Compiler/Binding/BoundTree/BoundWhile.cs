using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundWhile(BoundExpr condition, BoundExpr body, SyntaxNode? syntax = null)
    : BoundStmt(syntax)
{
    public BoundExpr Condition { get; } = condition;
    public BoundExpr Body { get; } = body;
}