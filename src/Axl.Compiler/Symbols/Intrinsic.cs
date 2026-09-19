namespace Axl.Compiler.Symbols;

public enum Intrinsic
{
    NegateI32,
    AddI32,
    SubtractI32,
    DivideI32,
    MultiplyI32,
    EqualsI32,
    LessThanI32,
    LessThanOrEqualI32,
    
    NegateI64,
    AddI64,
    SubtractI64,
    DivideI64,
    MultiplyI64,
    EqualsI64,
    LessThanI64,
    LessThanOrEqualI64,
    
    NegateF32,
    AddF32,
    SubtractF32,
    DivideF32,
    MultiplyF32,
    EqualsF32,
    LessThanF32,
    LessThanOrEqualF32,
    
    NegateF64,
    AddF64,
    SubtractF64,
    DivideF64,
    MultiplyF64,
    EqualsF64,
    LessThanF64,
    LessThanOrEqualF64,
    
    NotBool,
    EqualsBool,
    
    EqualsUnit,
    
    EqualsString,
}