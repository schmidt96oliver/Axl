using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundError(SyntaxNode? syntax = null) 
    : BoundExpr(ErrorTypeSymbol.Instance, syntax)
{
    protected override ImmutableArray<BoundStmt> GetChildren() => [];
}