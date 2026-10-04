using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundAssign(BoundValue target, BoundValue value, TypeSymbol type, SyntaxNode? syntax = null)
    : BoundValue(type, syntax)
{
    public BoundValue Target { get; } = target;
    public BoundValue Value { get; } = value;
    
}