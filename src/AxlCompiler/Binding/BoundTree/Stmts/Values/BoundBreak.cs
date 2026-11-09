using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundBreak(TypeSymbol type, SyntaxNode? syntax = null)
    : BoundValue(type, syntax)
{
    public override IEnumerable<BoundStmt> GetChildren()
        => [];
}