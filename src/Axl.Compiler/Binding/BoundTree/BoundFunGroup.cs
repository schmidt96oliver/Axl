using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

/// <summary>
/// Candidate fun symbols that go into overload resolution. They are all accessible
/// but have not been checked, for callability.
/// </summary>
public sealed class BoundFunGroup(ImmutableArray<FunSymbol> candidates, BoundValue? receiver, SyntaxNode syntax)
    : BoundNode(syntax)
{
    public ImmutableArray<FunSymbol> Candidates { get; } = candidates;
    public BoundValue? Receiver { get; } = receiver;

    /// <summary>
    /// Whether <see cref="Receiver"/> can be dropped, if the corresponding
    /// fun is static.
    /// </summary>
    public bool IsReceiverImplicit => Receiver is BoundSelf { IsImplicit: true };

    public string Name => Candidates[0].Name;
}