using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundTypeRef(TypeSymbol type, SyntaxNode syntax)
    : BoundNode(syntax), IBoundSymbolRef
{
    public TypeSymbol Type { get; } = type;

    /// <inheritdoc />
    public Symbol Symbol => Type;
}