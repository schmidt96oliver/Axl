using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundVariable(VariableSymbol variable, SyntaxNode? syntax = null) 
    : BoundValue(variable.Type, syntax), IBoundSymbolRef
{
    public VariableSymbol Variable { get; } = variable;
    
    /// <inheritdoc />
    public Symbol Symbol => Variable;

    /// <inheritdoc />
    public override PlaceKind PlaceKind => Variable.IsReadOnly ? PlaceKind.ReadOnlyPlace : PlaceKind.MutPlace;

    /// <inheritdoc />
    public override BoundValue PlaceRoot => this;

    public bool IsSelf => Variable is ParameterSymbol { IsSelf: true };

    public bool IsImplicitSelf => Variable is ParameterSymbol { IsSelf: true } && Syntax is null;


    /// <inheritdoc />
    public override IEnumerable<BoundStmt> GetChildren()
        => [];
}