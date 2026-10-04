using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundFunGroupRef(FunGroupSymbol funGroup, BoundValue? receiver, SyntaxNode? syntax)
    : BoundNode(syntax)
{
    public FunGroupSymbol FunGroup { get; } = funGroup;
    public BoundValue? Receiver { get; } = receiver;
}