using System.Collections.Immutable;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundLocal(LocalSymbol local, SyntaxNode? syntax = null) 
    : BoundValue(syntax), IBoundSymbolRef
{
    public LocalSymbol Local { get; } = local;
    
    /// <inheritdoc />
    public Symbol Symbol => Local;

    /// <inheritdoc />
    public override TypeSymbol Type => Local.Type;

    /// <inheritdoc />
    public override PlaceKind PlaceKind => Local.PlaceKind;

    /// <inheritdoc />
    public override ImmutableArray<BoundValue> PlacePath => [this];

    /// <inheritdoc />
    public override IEnumerable<BoundStmt> GetChildren()
        => [];
}