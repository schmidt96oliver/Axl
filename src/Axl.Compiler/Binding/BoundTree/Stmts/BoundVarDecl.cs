using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundVarDecl(VariableSymbol variable, BoundValue initializer, SyntaxNode? syntax = null) : BoundStmt(syntax)
{
    public VariableSymbol Variable { get; } = variable;
    public BoundValue Initializer { get; } = initializer;
}