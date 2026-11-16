using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundContinue(TypeSymbol type, SyntaxNode? syntax = null)
    : BoundValue(syntax)
{
    /// <inheritdoc />
    public override TypeSymbol Type { get; } = type;
    
    public override IEnumerable<BoundStmt> GetChildren()
        => [];
}