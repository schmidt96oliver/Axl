using System.Collections.Immutable;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Semantics.Types;

public record NativeOperatorInfo(TokenKind OpTokenKind, ImmutableArray<AxlType> OperandTypes, AxlType ReturnType, NativeOperatorKind NativeOperatorKind);

public enum NativeOperatorKind
{
    AddI32,
    AddI64,
    AddF32,
    AddF64,
    
    SubtractI32,
    SubtractI64,
    SubtractF32,
    SubtractF64,
    
    MultiplyI32,
    MultiplyI64,
    MultiplyF32,
    MultiplyF64,
    
    DivideI32,
    DivideI64,
    DivideF32,
    DivideF64,
    
    NegateI32,
    NegateI64,
    NegateF32,
    NegateF64,
    
    NotBool,
    
    LessThanI32,
    LessThanI64,
    LessThanF32,
    LessThanF64,
    
    LessThanOrEqualI32,
    LessThanOrEqualI64,
    LessThanOrEqualF32,
    LessThanOrEqualF64,
    
    GreaterThanI32,
    GreaterThanI64,
    GreaterThanF32,
    GreaterThanF64,
    
    GreaterThanOrEqualI32,
    GreaterThanOrEqualI64,
    GreaterThanOrEqualF32,
    GreaterThanOrEqualF64
}

public sealed class TypeContext
{
    private ImmutableArray<NativeOperatorInfo> _nativeOperatorInfos;
    
    public I32Type I32 { get; } = new();
    public I64Type I64 { get; } = new();
    public F32Type F32 { get; } = new();
    public F64Type F64 { get; } = new();
    public BoolType Bool { get; } = new();
    public StringType String { get; } = new();

    public NoneType None { get; } = new();
    public NeverType Never { get; } = new();
    public ErrorType Error { get; } = new();


    public AxlType DefaultIntegralNumberType => I32;

    public AxlType DefaultFloatingNumberType => F64;


    public TypeContext()
    {
        _nativeOperatorInfos = MakeNativeOperatorInfos();
    }
    

    /// <summary>
    /// Whether a value of type <paramref name="source"/> can be
    /// assigned to a target of type <paramref name="target"/>.
    /// </summary>
    public bool IsAssignableTo(AxlType source, AxlType target)
        => (source, target) switch
        {
            // Errors are silent
            (ErrorType, _) or (_, ErrorType) => true,

            // Never assigns to anything
            (NeverType, _) => true,

            // All other combination are exact match only
            _ => source == target
        };

    public NativeOperatorInfo? FindNativeOperator(TokenKind operatorTokenKind,
        params ImmutableArray<AxlType> operandTypes)
    {
        if (operandTypes.Length is < 1 or > 2)
            return null;

        return _nativeOperatorInfos.FirstOrDefault(opInfo => opInfo.OpTokenKind == operatorTokenKind &&
                                                             opInfo.OperandTypes.SequenceEqual(operandTypes));
    }


    private ImmutableArray<NativeOperatorInfo> MakeNativeOperatorInfos() =>
    [
        // Unary
        new(TokenKind.Minus, [I32], I32, NativeOperatorKind.NegateI32),
        new(TokenKind.Minus, [I64], I64, NativeOperatorKind.NegateI64),
        new(TokenKind.Minus, [F32], F32, NativeOperatorKind.NegateF32),
        new(TokenKind.Minus, [F64], F64, NativeOperatorKind.NegateF64),

        new(TokenKind.NotKw, [Bool], Bool, NativeOperatorKind.NotBool),

        // Binary math
        new(TokenKind.Plus, [I32, I32], I32, NativeOperatorKind.AddI32),
        new(TokenKind.Plus, [I64, I64], I64, NativeOperatorKind.AddI64),
        new(TokenKind.Plus, [F32, F32], F32, NativeOperatorKind.AddF32),
        new(TokenKind.Plus, [F64, F64], F64, NativeOperatorKind.AddF64),

        new(TokenKind.Minus, [I32, I32], I32, NativeOperatorKind.SubtractI32),
        new(TokenKind.Minus, [I64, I64], I64, NativeOperatorKind.SubtractI64),
        new(TokenKind.Minus, [F32, F32], F32, NativeOperatorKind.SubtractF32),
        new(TokenKind.Minus, [F64, F64], F64, NativeOperatorKind.SubtractF64),

        new(TokenKind.Star, [I32, I32], I32, NativeOperatorKind.MultiplyI32),
        new(TokenKind.Star, [I64, I64], I64, NativeOperatorKind.MultiplyI64),
        new(TokenKind.Star, [F32, F32], F32, NativeOperatorKind.MultiplyF32),
        new(TokenKind.Star, [F64, F64], F64, NativeOperatorKind.MultiplyF64),

        new(TokenKind.Slash, [I32, I32], I32, NativeOperatorKind.DivideI32),
        new(TokenKind.Slash, [I64, I64], I64, NativeOperatorKind.DivideI64),
        new(TokenKind.Slash, [F32, F32], F32, NativeOperatorKind.DivideF32),
        new(TokenKind.Slash, [F64, F64], F64, NativeOperatorKind.DivideF64),
        
        // Numeric comparison
        new(TokenKind.LessThan, [I32, I32], Bool, NativeOperatorKind.LessThanI32),
        new(TokenKind.LessThan, [I64, I64], Bool, NativeOperatorKind.LessThanI64),
        new(TokenKind.LessThan, [F32, F32], Bool, NativeOperatorKind.LessThanF32),
        new(TokenKind.LessThan, [F64, F64], Bool, NativeOperatorKind.LessThanF64),
        
        new(TokenKind.LessThanEqual, [I32, I32], Bool, NativeOperatorKind.LessThanOrEqualI32),
        new(TokenKind.LessThanEqual, [I64, I64], Bool, NativeOperatorKind.LessThanOrEqualI64),
        new(TokenKind.LessThanEqual, [F32, F32], Bool, NativeOperatorKind.LessThanOrEqualF32),
        new(TokenKind.LessThanEqual, [F64, F64], Bool, NativeOperatorKind.LessThanOrEqualF64),
        
        new(TokenKind.GreaterThan, [I32, I32], Bool, NativeOperatorKind.GreaterThanI32),
        new(TokenKind.GreaterThan, [I64, I64], Bool, NativeOperatorKind.GreaterThanI64),
        new(TokenKind.GreaterThan, [F32, F32], Bool, NativeOperatorKind.GreaterThanF32),
        new(TokenKind.GreaterThan, [F64, F64], Bool, NativeOperatorKind.GreaterThanF64),
        
        new(TokenKind.GreaterThanEqual, [I32, I32], Bool, NativeOperatorKind.GreaterThanOrEqualI32),
        new(TokenKind.GreaterThanEqual, [I64, I64], Bool, NativeOperatorKind.GreaterThanOrEqualI64),
        new(TokenKind.GreaterThanEqual, [F32, F32], Bool, NativeOperatorKind.GreaterThanOrEqualF32),
        new(TokenKind.GreaterThanEqual, [F64, F64], Bool, NativeOperatorKind.GreaterThanOrEqualF64),
    ];

}