using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundField(BoundValue receiver, FieldSymbol field, SyntaxNode? syntax = null)
    : BoundValue(field.Type, syntax), IBoundSymbolRef
{
    public BoundValue Receiver { get; } = receiver;
    public FieldSymbol Field { get; } = field;

    /// <inheritdoc />
    public Symbol Symbol => Field;

    /// <inheritdoc />
    public override PlaceKind PlaceKind => Receiver.PlaceKind;
        // At(1).X = 10;
    /// <inheritdoc />
    public override BoundValue? PlaceRoot => Receiver.PlaceRoot;

    public override IEnumerable<BoundStmt> GetChildren()
        => [Receiver];
}