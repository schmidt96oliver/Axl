using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

/// <summary>
/// A reference to a <see cref="VariableSymbol"/>. Note that failed lookups
/// are represented as <see cref="BoundError"/>.
/// </summary>
public sealed class BoundVariableRef(VariableSymbol variable, SyntaxNode syntax) : BoundExpr(variable.Type, syntax)
{
    public VariableSymbol Variable { get; } = variable;
    
    protected override ImmutableArray<BoundStmt> GetChildren() => [];
    
}