using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundTypeArgument(TypeSymbol type, SyntaxNode syntax)
{
    public TypeSymbol Type { get; } = type;
    public SyntaxNode Syntax { get; } = syntax;
}