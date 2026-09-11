using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirIf(HirExpr predicate, HirExpr then, HirExpr? @else, TypeSymbol type, SyntaxNode syntax)
    : HirExpr(type, syntax)
{
    public HirExpr Predicate { get; } = predicate;
    public HirExpr Then { get; } = then;
    public HirExpr? Else { get; } = @else;
    
    protected override ImmutableArray<HirStmt> GetChildren() 
        => Else is not null ? [Predicate, Then, Else] : [Predicate, Then];
    
}