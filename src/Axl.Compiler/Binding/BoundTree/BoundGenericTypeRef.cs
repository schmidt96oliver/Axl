using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

/// <summary>
/// A reference to a generic type where type arguments are not
/// filled yet.
/// </summary>
public sealed class BoundGenericTypeRef : BoundNode
{
    public BoundGenericTypeRef(ImmutableArray<TypeSymbol> candidates, SyntaxNode syntax) : base(syntax)
    {
        Guard.MustBe(candidates.All(t => t.TypeParameters.SequenceEqual(t.TypeArguments)),
            "Type arguments must not be filled.");
        Candidates = candidates;
    }

    public ImmutableArray<TypeSymbol> Candidates { get; }

    public TypeSymbol? NonGeneric => Candidates.FirstOrDefault(t => t.TypeParameters.Length == 0);
}