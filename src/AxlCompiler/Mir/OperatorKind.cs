using AxlCompiler.Symbols;

namespace AxlCompiler.Mir;

public enum OperatorKind
{
    AddWrappedI32,
    SubWrappedI32,
    DivUncheckedI32,
    MulWrappedI32,
    EqualsI32,
    LessThanI32,

    NegateI64,
    AddWrappedI64,
    SubWrappedI64,
    DivUncheckedI64,
    MulWrappedI64,
    EqualsI64,
    LessThanI64,

    AddF32,
    SubtractF32,
    DivideF32,
    MultiplyF32,
    EqualsF32,
    LessThanF32,

    AddF64,
    SubtractF64,
    DivideF64,
    MultiplyF64,
    EqualsF64,
    LessThanF64,

    NotBool,
    EqualsBool,
}

public static class OperatorKindExtensions
{
    extension(OperatorKind _)
    {
        public static OperatorKind? TryFrom(Intrinsic intrinsic) => intrinsic switch
        {
            Intrinsic.AddWrappedI32 => OperatorKind.AddWrappedI32,
            Intrinsic.SubWrappedI32 => OperatorKind.SubWrappedI32,
            Intrinsic.DivUncheckedI32 => OperatorKind.DivUncheckedI32,
            Intrinsic.MulWrappedI32 => OperatorKind.MulWrappedI32,
            Intrinsic.EqualsI32 => OperatorKind.EqualsI32,
            Intrinsic.LessThanI32 => OperatorKind.LessThanI32,

            Intrinsic.AddWrappedI64 => OperatorKind.AddWrappedI64,
            Intrinsic.SubWrappedI64 => OperatorKind.SubWrappedI64,
            Intrinsic.DivUncheckedI64 => OperatorKind.DivUncheckedI64,
            Intrinsic.MulWrappedI64 => OperatorKind.MulWrappedI64,
            Intrinsic.EqualsI64 => OperatorKind.EqualsI64,
            Intrinsic.LessThanI64 => OperatorKind.LessThanI64,

            Intrinsic.AddF32 => OperatorKind.AddF32,
            Intrinsic.SubF32 => OperatorKind.SubtractF32,
            Intrinsic.DivF32 => OperatorKind.DivideF32,
            Intrinsic.MulF32 => OperatorKind.MultiplyF32,
            Intrinsic.EqualsF32 => OperatorKind.EqualsF32,
            Intrinsic.LessThanF32 => OperatorKind.LessThanF32,

            Intrinsic.AddF64 => OperatorKind.AddF64,
            Intrinsic.SubF64 => OperatorKind.SubtractF64,
            Intrinsic.DivF64 => OperatorKind.DivideF64,
            Intrinsic.MulF64 => OperatorKind.MultiplyF64,
            Intrinsic.EqualsF64 => OperatorKind.EqualsF64,
            Intrinsic.LessThanF64 => OperatorKind.LessThanF64,

            Intrinsic.NotBool => OperatorKind.NotBool,
            Intrinsic.EqualsBool => OperatorKind.EqualsBool,
            
            _ => null,
        };
    }
}