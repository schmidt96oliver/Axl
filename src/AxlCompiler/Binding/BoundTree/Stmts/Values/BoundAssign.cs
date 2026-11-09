using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundAssign(BoundValue target, BoundValue value, TypeSymbol type, SyntaxNode? syntax = null)
    : BoundValue(type, syntax)
{
    public BoundValue Target { get; } = target;
    public BoundValue Value { get; } = value;

    public override IEnumerable<BoundStmt> GetChildren()
        => [Target, Value];
}