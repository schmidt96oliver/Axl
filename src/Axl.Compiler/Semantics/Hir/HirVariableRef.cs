using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Hir;

/// <summary>
/// A reference to a <see cref="VariableSymbol"/>. Note that failed lookups
/// are represented as <see cref="HirErrorExpr"/>.
/// </summary>
public sealed class HirVariableRef(VariableSymbol variable, SyntaxNode syntax) : HirExpr(variable.Type, syntax)
{
    public VariableSymbol Variable { get; } = variable;
    
    protected override ImmutableArray<HirStmt> GetChildren() => [];
    
}