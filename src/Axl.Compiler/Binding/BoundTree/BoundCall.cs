using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundCall(
    FunSymbol fun,
    BoundExpr? receiver,
    ImmutableArray<BoundExpr> arguments,
    SyntaxNode? syntax = null)
    : BoundExpr(fun.ReturnType, syntax)
{
    public FunSymbol Fun { get; } = fun;
    public BoundExpr? Receiver { get; } = receiver;
    public ImmutableArray<BoundExpr> Arguments { get; } = arguments;


    protected override ImmutableArray<BoundStmt> GetChildren()
        => Arguments.CastArray<BoundStmt>();
}