using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundVarDecl(VariableSymbol variable, BoundExpr initializer, SyntaxNode? syntax = null) : BoundStmt(syntax)
{
    public VariableSymbol Variable { get; } = variable;
    public BoundExpr Initializer { get; } = initializer;
}