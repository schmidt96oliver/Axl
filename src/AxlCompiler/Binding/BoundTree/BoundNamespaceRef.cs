using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundNamespaceRef(NamespaceSymbol @namespace, SyntaxNode syntax)
    : BoundNode(syntax), IBoundSymbolRef
{
    public NamespaceSymbol Namespace { get; } = @namespace;

    /// <inheritdoc />
    public Symbol Symbol => Namespace;
}