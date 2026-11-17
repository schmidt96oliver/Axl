using System.Collections.Immutable;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundStructInit(TypeSymbol @struct, ImmutableArray<BoundFieldInit> fieldInits, SyntaxNode? syntax)
    : BoundValue(syntax)
{
    public ImmutableArray<BoundFieldInit> FieldInits { get; } = fieldInits;

    /// <inheritdoc />
    public override TypeSymbol Type { get; } = @struct;

    public override IEnumerable<BoundStmt> GetChildren()
        => FieldInits.Select(i => i.Value);
}