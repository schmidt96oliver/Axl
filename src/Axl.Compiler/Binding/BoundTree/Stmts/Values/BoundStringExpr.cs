using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundStringExpr(ImmutableArray<BoundValue> parts, TypeSymbol type, SyntaxNode? syntax = null) : BoundValue(type, syntax)
{
    public ImmutableArray<BoundValue> Parts { get; } = parts;
}