using System.Collections.Immutable;
using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

/// <summary>
/// Candidate fun symbols that go into overload resolution. They are all accessible
/// but have not been checked, for callability.
/// </summary>
public sealed class BoundFunGroup(
    ImmutableArray<FunSymbol> candidates,
    BoundNode? receiver,
    ImmutableArray<BoundTypeRef> typeArguments,
    SyntaxNode syntax)
    : BoundNode(syntax)
{
    public BoundFunGroup(ImmutableArray<FunSymbol> candidates, BoundNode? receiver, SyntaxNode syntax)
        : this(candidates, receiver, [], syntax)
    {
    }

    public ImmutableArray<FunSymbol> Candidates { get; } = candidates;

    public ImmutableArray<BoundTypeRef> TypeArguments { get; } = typeArguments;

    public bool HasTypeArguments => TypeArguments.Length > 0;

    public BoundNode? Receiver { get; } = receiver;

    public BoundFunGroup WithTypeArguments(ImmutableArray<BoundTypeRef> typeArguments, SyntaxNode syntax)
        => new(Candidates, Receiver, typeArguments, syntax);
}