using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

/// <summary>
/// A reference to a <see cref="VariableSymbol"/>. Note that failed lookups
/// are represented as <see cref="BoundError"/>.
/// </summary>
public sealed class BoundVariable(VariableSymbol variable, SyntaxNode? syntax = null) 
    : BoundValue(variable.Type, syntax), IBoundSymbolRef
{
    public VariableSymbol Variable { get; } = variable;
    
    /// <inheritdoc />
    public Symbol Symbol => Variable;

    /// <inheritdoc />
    public override Place? Place => new(Root: this, IsMut: !Variable.IsReadOnly);

    public bool IsSelf => Variable is ParameterSymbol { IsSelf: true };

    public bool IsImplicitSelf => Variable is ParameterSymbol { IsSelf: true } && Syntax is null;
}