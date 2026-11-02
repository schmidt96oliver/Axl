using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

/// <summary>
/// A group of types that still need resolution. Could also be
/// a single generic type without its type arguments.
/// </summary>
public sealed class BoundTypeGroup(ImmutableArray<TypeSymbol> candidates, SyntaxNode syntax) : BoundNode(syntax)
{
    public ImmutableArray<TypeSymbol> Candidates { get; } = candidates;
}