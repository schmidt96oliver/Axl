using Axl.Compiler.Semantics.Symbols;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirVarDecl(LocalSymbol variableSymbol, HirExpr initializer) : HirStmt
{
    public LocalSymbol VariableSymbol { get; } = variableSymbol;
    public HirExpr Initializer { get; } = initializer;
}