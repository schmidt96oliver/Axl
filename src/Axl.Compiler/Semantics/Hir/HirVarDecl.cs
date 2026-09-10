using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirVarDecl(VariableSymbol variable, HirExpr initializer, SyntaxNode syntax) : HirStmt(syntax)
{
    public VariableSymbol Variable { get; } = variable;
    public HirExpr Initializer { get; } = initializer;
    
    protected override ImmutableArray<HirStmt> GetChildren() => [Initializer];
    
}