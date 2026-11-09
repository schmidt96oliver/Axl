using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public closed class BoundNode(SyntaxNode? syntax = null)
{
    public SyntaxNode? Syntax { get; } = syntax;
}