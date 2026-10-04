using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundAssign(VariableSymbol target, BoundValue value, TypeSymbol type, SyntaxNode? syntax = null)
    : BoundValue(type, syntax)
{
    public VariableSymbol Target { get; } = target;
    public BoundValue Value { get; } = value;
    
}