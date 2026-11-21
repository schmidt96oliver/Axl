using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundCompoundAssign(BoundValue target, BoundValue right, FunSymbol operatorFun,
    TypeSymbol type, SyntaxNode? syntax = null)
    : BoundValue(syntax)
{
    public BoundValue Target { get; } = target;
    public BoundValue Right { get; } = right;
    public FunSymbol OperatorFun { get; } = operatorFun;

    /// <inheritdoc />
    public override TypeSymbol Type { get; } = type;

    public override IEnumerable<BoundStmt> GetChildren()
        => [Target, Right];
}