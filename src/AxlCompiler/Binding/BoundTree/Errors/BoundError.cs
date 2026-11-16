using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public class BoundError(ErrorTypeSymbol type, SyntaxNode? syntax = null)
    : BoundValue(syntax)
{
    public override IEnumerable<BoundStmt> GetChildren()
        => [];

    /// <inheritdoc />
    public override TypeSymbol Type { get; } = type;
}