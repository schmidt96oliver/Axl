using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundNamespaceRef(NamespaceSymbol @namespace, SyntaxNode syntax)
    : BoundNode(syntax), IBoundSymbolRef
{
    public NamespaceSymbol Namespace { get; } = @namespace;

    /// <inheritdoc />
    public Symbol Symbol => Namespace;
}