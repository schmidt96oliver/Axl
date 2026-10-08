using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding.BoundTree;

public sealed class BoundSelf(TypeSymbol type, bool mutatable = false, SyntaxNode? syntax = null)
    : BoundValue(type, syntax)
{
    /// <summary>
    /// Self is always a place.
    /// </summary>
    public override bool IsPlace => true;

    /// <summary>
    /// Mutability depends on context: For 'var fun' it is mutable,
    /// for 'fun', it is not.
    /// </summary>
    public override bool IsMutablePlace => mutatable;

    /// <summary>
    /// Self is never directly assignable to.
    /// </summary>
    public override bool IsAssignable => false;
}