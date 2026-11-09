using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public interface IBoundSymbolRef
{
    public Symbol Symbol { get; }

    public SyntaxNode? Syntax { get; }
}