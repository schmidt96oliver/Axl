using Axl.Compiler.Symbols;

namespace Axl.Compiler.Mir;

public enum OperatorKind
{
    NegateI32,
    AddI32,
    SubtractI32,
    DivideI32,
    MultiplyI32,
    EqualsI32,
    LessThanI32,

    NegateI64,
    AddI64,
    SubtractI64,
    DivideI64,
    MultiplyI64,
    EqualsI64,
    LessThanI64,

    NegateF32,
    AddF32,
    SubtractF32,
    DivideF32,
    MultiplyF32,
    EqualsF32,
    LessThanF32,

    NegateF64,
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
            Intrinsic.NegateI32 => OperatorKind.NegateI32,
            Intrinsic.AddI32 => OperatorKind.AddI32,
            Intrinsic.SubtractI32 => OperatorKind.SubtractI32,
            Intrinsic.DivideI32 => OperatorKind.DivideI32,
            Intrinsic.MultiplyI32 => OperatorKind.MultiplyI32,
            Intrinsic.EqualsI32 => OperatorKind.EqualsI32,
            Intrinsic.LessThanI32 => OperatorKind.LessThanI32,

            Intrinsic.NegateI64 => OperatorKind.NegateI64,
            Intrinsic.AddI64 => OperatorKind.AddI64,
            Intrinsic.SubtractI64 => OperatorKind.SubtractI64,
            Intrinsic.DivideI64 => OperatorKind.DivideI64,
            Intrinsic.MultiplyI64 => OperatorKind.MultiplyI64,
            Intrinsic.EqualsI64 => OperatorKind.EqualsI64,
            Intrinsic.LessThanI64 => OperatorKind.LessThanI64,

            Intrinsic.NegateF32 => OperatorKind.NegateF32,
            Intrinsic.AddF32 => OperatorKind.AddF32,
            Intrinsic.SubtractF32 => OperatorKind.SubtractF32,
            Intrinsic.DivideF32 => OperatorKind.DivideF32,
            Intrinsic.MultiplyF32 => OperatorKind.MultiplyF32,
            Intrinsic.EqualsF32 => OperatorKind.EqualsF32,
            Intrinsic.LessThanF32 => OperatorKind.LessThanF32,

            Intrinsic.NegateF64 => OperatorKind.NegateF64,
            Intrinsic.AddF64 => OperatorKind.AddF64,
            Intrinsic.SubtractF64 => OperatorKind.SubtractF64,
            Intrinsic.DivideF64 => OperatorKind.DivideF64,
            Intrinsic.MultiplyF64 => OperatorKind.MultiplyF64,
            Intrinsic.EqualsF64 => OperatorKind.EqualsF64,
            Intrinsic.LessThanF64 => OperatorKind.LessThanF64,

            Intrinsic.NotBool => OperatorKind.NotBool,
            Intrinsic.EqualsBool => OperatorKind.EqualsBool,
            
            _ => null,
        };
    }
}