using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundModuleRef(BaseModuleSymbol module, SyntaxNode? syntax)
    : BoundNode(syntax)
{
    public BaseModuleSymbol Module { get; } = module;
}