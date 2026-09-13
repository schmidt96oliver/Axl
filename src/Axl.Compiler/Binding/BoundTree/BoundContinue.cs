using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundContinue(TypeSymbol type, SyntaxNode syntax)
    : BoundExpr(type, syntax)
{
    protected override ImmutableArray<BoundStmt> GetChildren()
        => [];
}