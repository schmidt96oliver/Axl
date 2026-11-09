using System.Collections.Immutable;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundStructInit(TypeSymbol type, ImmutableArray<BoundFieldInit> fieldInits, SyntaxNode? syntax)
    : BoundValue(type, syntax)
{
    public ImmutableArray<BoundFieldInit> FieldInits { get; } = fieldInits;

    public override IEnumerable<BoundStmt> GetChildren()
        => FieldInits;
}

public sealed class BoundFieldInit(FieldSymbol field, BoundValue value, SyntaxNode? syntax)
    : BoundValue(type: value.Type, syntax)
{
    public BoundValue Value { get; } = value;
    public FieldSymbol Field { get; } = field;

    public override IEnumerable<BoundStmt> GetChildren()
        => [Value];
}