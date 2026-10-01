using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundAssign(VariableSymbol target, BoundExpr value, TypeSymbol type, SyntaxNode? syntax = null)
    : BoundExpr(type, syntax)
{
    public VariableSymbol Target { get; } = target;
    public BoundExpr Value { get; } = value;
    
}