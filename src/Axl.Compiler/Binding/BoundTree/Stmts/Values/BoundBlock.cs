using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundBlock(
    ImmutableArray<BoundStmt> stmts,
    ImmutableArray<FunSymbol> localFuns,
    TypeSymbol type,
    SyntaxNode? syntax = null) : BoundValue(type, syntax)
{
    public ImmutableArray<BoundStmt> Stmts { get; } = stmts;
    public ImmutableArray<FunSymbol> LocalFuns { get; } = localFuns;

    /// <summary>
    /// Returns this block with <paramref name="additionalStmts"/> attached. Does
    /// not modify this block.
    /// </summary>
    public BoundBlock WithAdditionalStmts(params IEnumerable<BoundStmt> additionalStmts)
        => new([.. Stmts, .. additionalStmts], LocalFuns, Type, Syntax);
}