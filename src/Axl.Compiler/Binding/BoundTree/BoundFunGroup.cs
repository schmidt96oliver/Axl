using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

/// <summary>
/// Candidate fun symbols that go into overload resolution. They are all accessible
/// but have not been checked, for callability.
/// </summary>
public sealed class BoundFunGroup(ImmutableArray<FunSymbol> candidates, BoundNode? receiver, SyntaxNode? syntax = null)
    : BoundNode(syntax)
{
    public ImmutableArray<FunSymbol> Candidates { get; } = candidates;
    public BoundNode? Receiver { get; } = receiver;
}