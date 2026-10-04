using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundIfStmt(BoundValue condition, BoundStmt then, BoundStmt? @else, SyntaxNode? syntax = null)
    : BoundStmt(syntax)
{
    public BoundValue Condition { get; } = condition;
    public BoundStmt Then { get; } = then;
    public BoundStmt? Else { get; } = @else;
}