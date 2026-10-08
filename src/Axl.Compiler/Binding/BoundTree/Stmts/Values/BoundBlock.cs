using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundBlock(
    ImmutableArray<BoundStmt> stmts,
    ImmutableArray<TypeSymbol> localTypes,
    ImmutableArray<Symbol> localFuns,
    TypeSymbol type,
    SyntaxNode? syntax = null) : BoundValue(type, syntax)
{
    public ImmutableArray<BoundStmt> Stmts { get; } = stmts;
    public ImmutableArray<TypeSymbol> LocalTypes { get; } = localTypes;
    public ImmutableArray<Symbol> LocalFuns { get; } = localFuns;

}