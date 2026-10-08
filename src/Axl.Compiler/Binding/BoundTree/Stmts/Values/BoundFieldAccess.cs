using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundFieldAccess(BoundValue receiver, FieldSymbol field, SyntaxNode? syntax = null)
    : BoundValue(field.Type, syntax)
{
    public BoundValue Receiver { get; } = receiver;
    public FieldSymbol Field { get; } = field;

    /// <summary>
    /// A field itself is a storage location. For the entire value to be a storage
    /// location, its receiver must be a storage location as well.
    /// </summary>
    public override bool IsPlace => Receiver.IsPlace;

    /// <summary>
    /// Same as <see cref="IsPlace"/>. A field can only be mutated through
    /// an assignable receiver.
    /// </summary>
    public override bool IsMutablePlace => Receiver.IsMutablePlace;

    /// <summary>
    /// Fields can be assigned to if they are mutable.
    /// </summary>
    public override bool IsAssignable => IsMutablePlace;
}