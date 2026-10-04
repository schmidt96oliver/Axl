using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public closed class BoundNode(SyntaxNode? syntax = null)
{
    public SyntaxNode? Syntax { get; } = syntax;
}