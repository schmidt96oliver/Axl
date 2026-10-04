using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundCall(
    FunSymbol fun,
    BoundValue? receiver,
    ImmutableArray<BoundValue> arguments,
    SyntaxNode? syntax = null)
    : BoundValue(fun.ReturnType, syntax)
{
    public FunSymbol Fun { get; } = fun;
    public BoundValue? Receiver { get; } = receiver;
    public ImmutableArray<BoundValue> Arguments { get; } = arguments;
}