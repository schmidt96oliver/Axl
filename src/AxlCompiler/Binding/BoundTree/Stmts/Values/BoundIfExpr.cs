using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundIfExpr(BoundValue condition, BoundValue then, BoundValue @else, TypeSymbol type,
    SyntaxNode? syntax = null)
    : BoundValue(type, syntax)
{
    public BoundValue Condition { get; } = condition;
    public BoundValue Then { get; } = then;
    public BoundValue Else { get; } = @else;

    public override IEnumerable<BoundStmt> GetChildren()
        => [Condition, Then, Else];
}