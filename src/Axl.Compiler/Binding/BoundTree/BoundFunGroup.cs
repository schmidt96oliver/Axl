using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

/// <summary>
/// Candidate fun symbols that go into overload resolution. They are all accessible
/// but have not been checked, for callability.
/// </summary>
public sealed class BoundFunGroup(
    ImmutableArray<FunSymbol> candidates,
    BoundNode? receiver,
    ImmutableArray<BoundTypeArgument> typeArguments,
    SyntaxNode? typeArgListSyntax = null,
    SyntaxNode? syntax = null)
    : BoundNode(syntax)
{
    public BoundFunGroup(ImmutableArray<FunSymbol> candidates, BoundNode? receiver, SyntaxNode? syntax = null)
        : this(candidates, receiver, [], null, syntax)
    {
    }

    public ImmutableArray<FunSymbol> Candidates { get; } = candidates;

    public ImmutableArray<BoundTypeArgument> TypeArguments { get; } = typeArguments;
    public SyntaxNode? TypeArgListSyntax { get; } = typeArgListSyntax;

    public bool HasTypeArguments => TypeArguments.Length > 0;
    
    public BoundNode? Receiver { get; } = receiver;

    public BoundFunGroup WithTypeArguments(ImmutableArray<BoundTypeArgument> typeArguments, SyntaxNode? typeArgListSyntax, SyntaxNode? syntax = null)
        => new(Candidates, Receiver, typeArguments, typeArgListSyntax, syntax ?? Syntax);
}