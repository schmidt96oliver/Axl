using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundBreak(NeverTypeSymbol type, SyntaxNode? syntax = null)
    : BoundValue(type, syntax)
{
    public override IEnumerable<BoundStmt> GetChildren()
        => [];
}