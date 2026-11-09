using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundReturn(BoundValue? value, bool isMut, TypeSymbol type, SyntaxNode? syntax = null)
    : BoundValue(type, syntax)
{
    public BoundValue? Value { get; } = value;
    public bool IsMut { get; } = isMut;

    public override IEnumerable<BoundStmt> GetChildren()
        => Value is not null ? [Value] : [];
}