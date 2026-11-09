using AxlCompiler.Symbols;
using AxlCompiler.Syntax;

namespace AxlCompiler.Binding.BoundTree;

public sealed class BoundVarDecl(VariableSymbol variable, BoundValue initializer, SyntaxNode? syntax = null) : BoundStmt(syntax)
{
    public VariableSymbol Variable { get; } = variable;
    public BoundValue Initializer { get; } = initializer;

    public override IEnumerable<BoundStmt> GetChildren()
        => [Initializer];
}