using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundIfExpr(BoundExpr condition, BoundExpr then, BoundExpr @else, TypeSymbol type, SyntaxNode syntax)
    : BoundExpr(type, syntax)
{
    public BoundExpr Condition { get; } = condition;
    public BoundExpr Then { get; } = then;
    public BoundExpr Else { get; } = @else;
    
    protected override ImmutableArray<BoundStmt> GetChildren() 
        => [Condition, Then, Else];
    
}