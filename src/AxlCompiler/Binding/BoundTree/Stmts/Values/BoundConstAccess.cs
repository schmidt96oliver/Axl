using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundConstAccess(ConstSymbol constSymbol, SyntaxNode? syntax) 
    : BoundValue(constSymbol.Type, syntax), IBoundSymbolRef
{
    public ConstSymbol ConstSymbol { get; } = constSymbol;

    /// <inheritdoc />
    public override IEnumerable<BoundStmt> GetChildren()
        => [];

    /// <inheritdoc />
    public Symbol Symbol => ConstSymbol;
}