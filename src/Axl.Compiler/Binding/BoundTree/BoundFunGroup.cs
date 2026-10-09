using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundFunGroup(Binder.LookupResult lookupResult, bool allowImplicitSelf, BoundValue? receiver, SyntaxNode syntax)
    : BoundNode(syntax)
{
    public Binder.LookupResult LookupResult { get; } = lookupResult;
    public bool AllowImplicitSelf { get; } = allowImplicitSelf;
    public BoundValue? Receiver { get; } = receiver;

    public string Name => LookupResult.Symbols[0].Name;
}