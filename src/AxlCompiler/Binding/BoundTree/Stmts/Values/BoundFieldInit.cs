using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundFieldInit(FieldSymbol field, BoundValue value, SyntaxNode? syntax)
    //: BoundValue(type: value.Type, syntax)
{
    public BoundValue Value { get; } = value;
    public SyntaxNode? Syntax { get; } = syntax;
    public FieldSymbol Field { get; } = field;
}