namespace AxlCompiler.Symbols;

public enum Intrinsic
{
    // !! When adding intrinsics, also adjust OperatorKind and VmCallKind in Lowering !!
    
    AddWrappedI32,
    SubWrappedI32,
    DivUncheckedI32,
    MulWrappedI32,
    EqualsI32,
    LessThanI32,
    ToStringI32,
    
    NegateI64,
    AddI64,
    SubtractI64,
    DivideI64,
    MultiplyI64,
    EqualsI64,
    LessThanI64,
    ToStringI64,
    
    NegateF32,
    AddF32,
    SubtractF32,
    DivideF32,
    MultiplyF32,
    EqualsF32,
    LessThanF32,
    ToStringF32,
    
    NegateF64,
    AddF64,
    SubtractF64,
    DivideF64,
    MultiplyF64,
    EqualsF64,
    LessThanF64,
    ToStringF64,
    
    NotBool,
    EqualsBool,
    ToStringBool,
    
    EqualsString,
    ConcatString,
    
    Print,
    Panic,
}