using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundCall(FunSymbol fun, ImmutableArray<BoundExpr> arguments, TypeSymbol type, SyntaxNode syntax)
    : BoundExpr(type, syntax)
{
    public FunSymbol Fun { get; } = fun;
    public ImmutableArray<BoundExpr> Arguments { get; } = arguments;

    protected override ImmutableArray<BoundStmt> GetChildren()
        => Arguments.CastArray<BoundStmt>();
}