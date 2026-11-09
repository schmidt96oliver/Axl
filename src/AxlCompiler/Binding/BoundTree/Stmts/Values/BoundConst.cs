using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public union ConstValue(int, long, float, double, bool, string);

public sealed class BoundConst(ConstValue value, TypeSymbol type, SyntaxNode? syntax = null) 
    : BoundValue(type, syntax)
{
    public ConstValue Value { get; } = value;

    public override IEnumerable<BoundStmt> GetChildren()
        => [];
}