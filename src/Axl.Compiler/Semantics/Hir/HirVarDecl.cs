using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirVarDecl(LocalSymbol variableSymbol, HirExpr initializer, SyntaxNode syntax) : HirStmt(syntax)
{
    public LocalSymbol VariableSymbol { get; } = variableSymbol;
    public HirExpr Initializer { get; } = initializer;
}