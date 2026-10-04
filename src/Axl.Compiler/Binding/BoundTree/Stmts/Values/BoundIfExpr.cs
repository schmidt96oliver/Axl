using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundIfExpr(BoundValue condition, BoundValue then, BoundValue @else, TypeSymbol type,
    SyntaxNode? syntax = null)
    : BoundValue(type, syntax)
{
    public BoundValue Condition { get; } = condition;
    public BoundValue Then { get; } = then;
    public BoundValue Else { get; } = @else;
}