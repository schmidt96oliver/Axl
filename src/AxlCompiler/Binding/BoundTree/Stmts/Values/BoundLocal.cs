using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundLocal(LocalSymbol local, SyntaxNode? syntax = null) 
    : BoundValue(local.Type, syntax), IBoundSymbolRef
{
    public LocalSymbol Local { get; } = local;
    
    /// <inheritdoc />
    public Symbol Symbol => Local;

    /// <inheritdoc />
    public override PlaceKind PlaceKind => Local.PlaceKind;

    /// <inheritdoc />
    public override BoundValue PlaceRoot => this;

    /// <inheritdoc />
    public override IEnumerable<BoundStmt> GetChildren()
        => [];
}