using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

/// <summary>
/// A reference to a <see cref="VariableSymbol"/>. Note that failed lookups
/// are represented as <see cref="BoundError"/>.
/// </summary>
public sealed class BoundVariable(VariableSymbol variable, SyntaxNode? syntax = null) : BoundValue(variable.Type, syntax)
{
    public VariableSymbol Variable { get; } = variable;

    /// <summary>
    /// A variable always refers to a storage location.
    /// </summary>
    public override bool IsPlace => true;

    /// <summary>
    /// A variable is mutable, if it came from a 'var' binding.
    /// </summary>
    public override bool IsMutablePlace => !Variable.IsReadOnly;

    /// <summary>
    /// A variable is assignable, if it came from a 'var' binding.
    /// </summary>
    public override bool IsAssignable => IsMutablePlace;
}