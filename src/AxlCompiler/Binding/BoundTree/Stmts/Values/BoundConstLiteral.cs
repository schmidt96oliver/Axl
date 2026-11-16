using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public union ConstValue(int, long, float, double, bool, string);

public sealed class BoundConstLiteral(ConstValue value, TypeSymbol type, SyntaxNode? syntax = null) 
    : BoundValue(syntax)
{
    public ConstValue Value { get; } = value;

    /// <inheritdoc />
    public override TypeSymbol Type { get; } = type;

    public override IEnumerable<BoundStmt> GetChildren()
        => [];
}