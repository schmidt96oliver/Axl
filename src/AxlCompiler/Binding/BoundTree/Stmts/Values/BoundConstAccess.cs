using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundConstAccess(ConstSymbol constSymbol, SyntaxNode? syntax) 
    : BoundValue(syntax), IBoundSymbolRef
{
    public ConstSymbol ConstSymbol { get; } = constSymbol;

    /// <inheritdoc />
    public override TypeSymbol Type => ConstSymbol.Type;

    /// <inheritdoc />
    public override IEnumerable<BoundStmt> GetChildren()
        => [];

    /// <inheritdoc />
    public Symbol Symbol => ConstSymbol;
}