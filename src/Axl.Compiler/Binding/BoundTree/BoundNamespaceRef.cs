using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundNamespaceRef(NamespaceSymbol @namespace, SyntaxNode? syntax, SyntaxNode? memberSyntax)
    : BoundNode(syntax)
{
    public NamespaceSymbol Namespace { get; } = @namespace;

    /// <summary>
    /// Syntax that refers to the symbol itself rather than the entire
    /// expression that yielded this symbol.
    /// E.g. in `Base.I32`, <see cref="MemberSyntax"/> refers to `I32`,
    /// while <see cref="Syntax"/> refers to `Base.I32`.
    /// </summary>
    public SyntaxNode? MemberSyntax { get; } = memberSyntax;
}