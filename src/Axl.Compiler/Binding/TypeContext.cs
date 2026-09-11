using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding;

public sealed class TypeContext
{
    private readonly ImmutableArray<NativeOperatorInfo> _nativeOperatorInfos;
    
    public TypeSymbol I32 { get; } = new("i32");
    public TypeSymbol I64 { get; } = new("i64");
    public TypeSymbol F32 { get; } = new("f32");
    public TypeSymbol F64 { get; } = new("f64");
    public TypeSymbol Bool { get; } = new("bool");
    public TypeSymbol String { get; } = new("string");

    public TypeSymbol None { get; } = new("none");
    public TypeSymbol Never { get; } = new("never");
    public TypeSymbol Error { get; } = new("???");


    public TypeSymbol DefaultIntegralNumberType => I32;

    public TypeSymbol DefaultFloatingNumberType => F64;


    public TypeContext()
    {
        _nativeOperatorInfos = MakeNativeOperatorInfos();
    }


    /// <summary>
    /// Whether a value of type <paramref name="source"/> can be
    /// assigned to a target of type <paramref name="target"/>.
    /// </summary>
    public bool IsAssignableTo(TypeSymbol source, TypeSymbol target)
    {
        // Errors are silent
        if (source == Error || target == Error) return true;
        
        // Never assigns to anything
        if (source == Never) return true;

        return source == target;
    }

    public NativeOperatorInfo? TryGetNativeOperator(TokenKind operatorTokenKind,
        params ImmutableArray<TypeSymbol> operandTypes)
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