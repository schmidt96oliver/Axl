using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundReturn(BoundValue? value, SyntaxNode? syntax = null)
    : BoundValue(NeverTypeSymbol.Instance, syntax)
{
    public BoundValue? Value { get; } = value;
}