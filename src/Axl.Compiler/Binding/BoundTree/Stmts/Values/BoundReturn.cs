using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundReturn(BoundValue? value, bool isMut, SyntaxNode? syntax = null)
    : BoundValue(NeverTypeSymbol.Instance, syntax)
{
    public BoundValue? Value { get; } = value;
    public bool IsMut { get; } = isMut;

    public override IEnumerable<BoundStmt> GetChildren()
        => Value is not null ? [Value] : [];
}