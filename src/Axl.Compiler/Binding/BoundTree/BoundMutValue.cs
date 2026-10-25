using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundMutValue(BoundValue inner, SyntaxNode? syntax = null) : BoundNode(syntax)
{
    public BoundValue Inner { get; } = inner;
}