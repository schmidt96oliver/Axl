using System.Collections.Immutable;
using Axl.Compiler.Semantics.Symbols;
using Axl.Compiler.Semantics.Types;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

public sealed class HirAssign(VariableSymbol target, HirExpr value, AxlType type, SyntaxNode syntax)
    : HirExpr(type, syntax)
{
    public VariableSymbol Target { get; } = target;
    public HirExpr Value { get; } = value;
    
    protected override ImmutableArray<HirStmt> GetChildren() => [Value];
    
}