using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Mir;

public closed record Terminator(SyntaxNode? Syntax)
{
    public sealed record Goto(MutBasicBlock Block, SyntaxNode? Syntax = null)
        : Terminator(Syntax);
    
    public sealed record Branch(Operand Condition, MutBasicBlock True, MutBasicBlock False, SyntaxNode? Syntax = null)
        : Terminator(Syntax);
    
    public sealed record Return(SyntaxNode? Syntax = null)
        : Terminator(Syntax);

    public sealed record MissingReturn() : Terminator(Syntax: null);
    
    public sealed record Unreachable(SyntaxNode? Syntax = null)
        : Terminator(Syntax);
}