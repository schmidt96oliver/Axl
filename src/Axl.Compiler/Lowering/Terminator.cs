using Axl.Compiler.Syntax;

namespace Axl.Compiler.Lowering;

public closed record Terminator(SyntaxNode? Syntax)
{
    public sealed record Goto(BasicBlock Block, SyntaxNode? Syntax = null)
        : Terminator(Syntax);
    
    public sealed record Branch(Operand Condition, BasicBlock True, BasicBlock False, SyntaxNode? Syntax = null)
        : Terminator(Syntax);
    
    public sealed record Return(SyntaxNode? Syntax = null)
        : Terminator(Syntax);
    
    public sealed record Unreachable(SyntaxNode? Syntax = null)
        : Terminator(Syntax);
}