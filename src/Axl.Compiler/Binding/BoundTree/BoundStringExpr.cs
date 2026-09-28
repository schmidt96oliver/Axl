using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundStringExpr(ImmutableArray<BoundExpr> parts, TypeSymbol type, SyntaxNode syntax) : BoundExpr(type, syntax)
{
    public ImmutableArray<BoundExpr> Parts { get; } = parts;

    protected override ImmutableArray<BoundStmt> GetChildren()
        => Parts.CastArray<BoundStmt>();

}