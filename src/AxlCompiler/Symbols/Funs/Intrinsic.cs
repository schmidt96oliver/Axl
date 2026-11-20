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
    
    AddWrappedI64,
    SubWrappedI64,
    DivUncheckedI64,
    MulWrappedI64,
    EqualsI64,
    LessThanI64,
    ToStringI64,
    
    AddF32,
    SubF32,
    DivF32,
    MulF32,
    EqualsF32,
    LessThanF32,
    ToStringF32,
    
    AddF64,
    SubF64,
    DivF64,
    MulF64,
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
    
    BufferNew,
    BufferAt
}