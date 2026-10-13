using Axl.Compiler.Symbols;

namespace Axl.Compiler.Binding.BoundTree;

/// <summary>
/// An expression that can be interpreted both as a value or as a type.
/// If not handled otherwise, it should be interpreted as the value
/// per default.
/// </summary>
/// <example>
/// <code>
/// struct A { Color: Color; }
/// </code>
/// Inside <c>A</c>, <c>Color.Black</c> might refer to
/// a static field on the type Color and <c>Color.GetHue()</c>
/// might refer to the field Color.
/// </example>
public sealed class BoundValueOrTypeRef : BoundValue
{
    public BoundValue AsValue { get; }
    
    public BoundTypeRef AsTypeRef { get; }

    public BoundValueOrTypeRef(BoundValue asValue, BoundTypeRef asTypeRef)
        : base(asValue.Type, asValue.Syntax)
    {
        Guard.MustBe(asValue.Syntax == asTypeRef.Syntax);
        Guard.MustBe(asValue.Type == asTypeRef.Type);
        
        AsValue = asValue;
        AsTypeRef = asTypeRef;
    }
}