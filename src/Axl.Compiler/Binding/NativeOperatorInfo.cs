using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding;

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

public record NativeOperatorInfo(TokenKind OpTokenKind, ImmutableArray<TypeSymbol> OperandTypes, TypeSymbol ReturnType, NativeOperatorKind Kind);