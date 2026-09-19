using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundCall(IntrinsicFunSymbol fun, ImmutableArray<BoundExpr> arguments, TypeSymbol type, SyntaxNode syntax)
    : BoundExpr(type, syntax)
{
    public IntrinsicFunSymbol Fun { get; } = fun;
    public ImmutableArray<BoundExpr> Arguments { get; } = arguments;

    protected override ImmutableArray<BoundStmt> GetChildren()
        => Arguments.CastArray<BoundStmt>();
}