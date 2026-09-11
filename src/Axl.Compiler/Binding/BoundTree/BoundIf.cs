using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundIf(BoundExpr predicate, BoundExpr then, BoundExpr? @else, TypeSymbol type, SyntaxNode syntax)
    : BoundExpr(type, syntax)
{
    public BoundExpr Predicate { get; } = predicate;
    public BoundExpr Then { get; } = then;
    public BoundExpr? Else { get; } = @else;
    
    protected override ImmutableArray<BoundStmt> GetChildren() 
        => Else is not null ? [Predicate, Then, Else] : [Predicate, Then];
    
}