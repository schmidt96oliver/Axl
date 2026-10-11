using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundFunGroup(ImmutableArray<FunSymbol> candidates, bool allowImplicitSelf, BoundValue? receiver, SyntaxNode syntax)
    : BoundNode(syntax)
{
    public ImmutableArray<FunSymbol> Candidates { get; } = candidates;
    public bool AllowImplicitSelf { get; } = allowImplicitSelf;
    public BoundValue? Receiver { get; } = receiver;

    public string Name => Candidates[0].Name;
}