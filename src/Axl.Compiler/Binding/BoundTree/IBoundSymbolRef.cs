using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public interface IBoundSymbolRef
{
    public Symbol Symbol { get; }

    public SyntaxNode? Syntax { get; }
}