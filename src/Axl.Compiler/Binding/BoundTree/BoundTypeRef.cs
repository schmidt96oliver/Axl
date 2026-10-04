using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundTypeRef(TypeSymbol type, SyntaxNode? syntax)
    : BoundNode(syntax)
{
    public TypeSymbol Type { get; } = type;
}