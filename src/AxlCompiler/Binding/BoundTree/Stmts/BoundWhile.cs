using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundWhile(BoundValue condition, BoundValue body, SyntaxNode? syntax = null)
    : BoundStmt(syntax)
{
    public BoundValue Condition { get; } = condition;
    public BoundValue Body { get; } = body;

    public override IEnumerable<BoundStmt> GetChildren()
        => [Condition, Body];
}