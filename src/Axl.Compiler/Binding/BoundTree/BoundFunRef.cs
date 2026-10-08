using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundFunRef(FunSymbol fun, BoundValue? receiver, SyntaxNode syntax, SyntaxNode memberSyntax)
    : BoundNode(syntax)
{
    public FunSymbol Fun { get; } = fun;
    public BoundValue? Receiver { get; } = receiver;

    /// <summary>
    /// Syntax that refers to the symbol itself rather than the entire
    /// expression that yielded this symbol.
    /// E.g. in `Base.I32`, <see cref="MemberSyntax"/> refers to `I32`,
    /// while <see cref="Syntax"/> refers to `Base.I32`.
    /// </summary>
    public SyntaxNode MemberSyntax { get; } = memberSyntax;
}