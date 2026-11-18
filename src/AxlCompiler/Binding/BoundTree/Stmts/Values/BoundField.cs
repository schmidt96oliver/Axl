using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundField(BoundValue receiver, FieldSymbol field, SyntaxNode? syntax = null)
    : BoundValue(syntax), IBoundSymbolRef
{
    public BoundValue Receiver { get; } = receiver;
    public FieldSymbol Field { get; } = field;

    /// <inheritdoc />
    public override TypeSymbol Type => Field.Type;

    /// <inheritdoc />
    public Symbol Symbol => Field;

    /// <inheritdoc />
    public override PlaceKind PlaceKind => Receiver.PlaceKind switch
    {
        PlaceKind.MutPlace => PlaceKind.MutPlace,
        PlaceKind.ReadOnlyPlace or PlaceKind.Value when Receiver.Type.IsReferenceType => PlaceKind.MutPlace,
        _ => Receiver.PlaceKind
    };
        
    /// <inheritdoc />
    public override BoundValue? PlaceRoot => Receiver.PlaceRoot;

    public override IEnumerable<BoundStmt> GetChildren()
        => [Receiver];
}