using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundStructInit(StructSymbol @struct, ImmutableArray<BoundFieldInit> fieldInits, SyntaxNode? syntax)
    : BoundValue(type: @struct, syntax)
{
    public StructSymbol Struct { get; } = @struct;
    public ImmutableArray<BoundFieldInit> FieldInits { get; } = fieldInits;
}

public sealed class BoundFieldInit(FieldSymbol field, BoundValue value, SyntaxNode? syntax)
    : BoundValue(type: value.Type, syntax)
{
    public BoundValue Value { get; } = value;
    public FieldSymbol Field { get; } = field;
}