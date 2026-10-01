using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public union ConstValue(int, long, float, double, bool, string);

public sealed class BoundConst(ConstValue value, TypeSymbol type, SyntaxNode? syntax = null) 
    : BoundExpr(type, syntax)
{
    public ConstValue Value { get; } = value;
}