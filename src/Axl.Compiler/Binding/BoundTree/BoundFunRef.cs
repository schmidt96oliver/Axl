using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundFunRef(FunSymbol fun, BoundValue? receiver, SyntaxNode? syntax)
    : BoundNode(syntax)
{
    public FunSymbol Fun { get; } = fun;
    public BoundValue? Receiver { get; } = receiver;
}