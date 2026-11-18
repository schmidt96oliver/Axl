using System.Collections.Immutable;
using System.Diagnostics;
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
        _ when Receiver.Type.IsReferenceType
            => PlaceKind.MutPlace,
        
        _ => Receiver.PlaceKind
    };
        
    /// <inheritdoc />
    public override ImmutableArray<BoundValue> PlacePath => Receiver.PlaceKind switch
    {
        // When the receiver is a reference type, the path
        // starts there. No matter what came before.
        _ when Receiver.Type.IsReferenceType
            => [Receiver, this],
        
        // Receiver places just extend the path
        PlaceKind.MutPlace or PlaceKind.ReadOnlyPlace
            => [.. Receiver.PlacePath, this],

        // For values (of non-reference types), this is not a place at all.
        PlaceKind.Value => [],
        
        _ => throw new UnreachableException()
    };

    public override IEnumerable<BoundStmt> GetChildren()
        => [Receiver];
}