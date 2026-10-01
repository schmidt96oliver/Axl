using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public closed class BoundStmt(SyntaxNode? syntax = null)
{
    public SyntaxNode? Syntax { get; } = syntax;
}