using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundContinue(SyntaxNode? syntax = null)
    : BoundExpr(NeverTypeSymbol.Instance, syntax)
{
    protected override ImmutableArray<BoundStmt> GetChildren()
        => [];
}