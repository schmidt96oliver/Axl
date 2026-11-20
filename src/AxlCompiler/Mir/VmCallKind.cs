using AxlCompiler.Symbols;

namespace AxlCompiler.Mir;

public enum VmCallKind
{
    ToStringI32,
    ToStringI64,
    ToStringF32,
    ToStringF64,
    ToStringBool,

    ConcatString,
    EqualsString,

    Print,
    Panic,
    
    UnsafeMutBufferNewUninitialized,
    UnsafeMutBufferAtUnchecked,
    UnsafeMutBufferLength,
    
    /// <summary>
    /// (self: UnsafeMutBuffer[T], target: UnsafeMutBuffer[T], targetStart: I32, sourceStart: I32, length: I32, elementSize: I32)
    /// </summary>
    UnsafeMutBufferCopyIntoUnchecked,
}

public static class VmCallExtensions
{
    extension(VmCallKind _)
    {
        public static VmCallKind? TryFrom(Intrinsic intrinsic) => intrinsic switch
        {
            Intrinsic.ToStringI32 => VmCallKind.ToStringI32,
            Intrinsic.ToStringI64 => VmCallKind.ToStringI64,
            Intrinsic.ToStringF32 => VmCallKind.ToStringF32,
            Intrinsic.ToStringF64 => VmCallKind.ToStringF64,
            Intrinsic.ToStringBool => VmCallKind.ToStringBool,

            Intrinsic.ConcatString => VmCallKind.ConcatString,
            Intrinsic.EqualsString => VmCallKind.EqualsString,

            Intrinsic.Print => VmCallKind.Print,
            Intrinsic.Panic => VmCallKind.Panic,
            
            Intrinsic.UnsafeMutBufferNewUninitialized => VmCallKind.UnsafeMutBufferNewUninitialized,
            Intrinsic.UnsafeMutBufferAtUnchecked => VmCallKind.UnsafeMutBufferAtUnchecked,
            Intrinsic.UnsafeMutBufferLength => VmCallKind.UnsafeMutBufferLength,
            Intrinsic.UnsafeMutBufferCopyIntoUnchecked => VmCallKind.UnsafeMutBufferCopyIntoUnchecked,

            _ => null,
        };
    }
}