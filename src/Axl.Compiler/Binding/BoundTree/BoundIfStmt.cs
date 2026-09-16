using System.Collections.Immutable;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundIfStmt(BoundExpr condition, BoundStmt then, BoundStmt? @else, SyntaxNode syntax)
    : BoundStmt(syntax)
{
    public BoundExpr Condition { get; } = condition;
    public BoundStmt Then { get; } = then;
    public BoundStmt? Else { get; } = @else;
    
    protected override ImmutableArray<BoundStmt> GetChildren() 
        => Else is not null ? [Condition, Then, Else] : [Condition, Then];
    
}