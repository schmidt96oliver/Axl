using System.Collections.Immutable;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundStructInit(TypeSymbol type, ImmutableArray<BoundFieldInit> fieldInits, SyntaxNode? syntax)
    : BoundValue(syntax)
{
    public ImmutableArray<BoundFieldInit> FieldInits { get; } = fieldInits;

    /// <inheritdoc />
    public override TypeSymbol Type { get; } = type;

    public override IEnumerable<BoundStmt> GetChildren()
        => FieldInits.Select(i => i.Value);
}

public sealed class BoundFieldInit(FieldSymbol field, BoundValue value, SyntaxNode? syntax)
    //: BoundValue(type: value.Type, syntax)
{
    public BoundValue Value { get; } = value;
    public SyntaxNode? Syntax { get; } = syntax;
    public FieldSymbol Field { get; } = field;
}