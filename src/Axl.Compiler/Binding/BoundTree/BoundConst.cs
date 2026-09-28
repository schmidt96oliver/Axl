using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public union ConstValue(int, long, float, double, bool);

public sealed class BoundConst(ConstValue value, TypeSymbol type, SyntaxNode syntax) 
    : BoundExpr(type, syntax)
{
    public ConstValue Value { get; } = value;

    protected override ImmutableArray<BoundStmt> GetChildren()
        => [];
}