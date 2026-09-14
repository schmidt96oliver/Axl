using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundWhile(BoundExpr condition, BoundExpr body, TypeSymbol type, SyntaxNode syntax)
    : BoundExpr(type, syntax)
{
    public BoundExpr Condition { get; } = condition;
    public BoundExpr Body { get; } = body;
    
    protected override ImmutableArray<BoundStmt> GetChildren() 
        => [Condition, Body];
    
}