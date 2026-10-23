using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Binding;

public static class IntrinsicLookup
{
    private record IntrinsicDefinition(
        Intrinsic Intrinsic,
        string Name,
        PrimitiveKind? Receiver,
        ImmutableArray<PrimitiveKind> Parameters,
        PrimitiveKind? ReturnType)
    {
        public IntrinsicDefinition(Intrinsic Intrinsic, string Name, ImmutableArray<PrimitiveKind> Parameters)
            : this(Intrinsic, Name, null, Parameters, null)
        {
            
        }
        
        public IntrinsicDefinition(Intrinsic Intrinsic, TokenKind tokenName, PrimitiveKind Receiver, ImmutableArray<PrimitiveKind> Parameters, PrimitiveKind ReturnType)
            : this(Intrinsic, SyntaxFacts.GetText(tokenName)!, Receiver, Parameters, ReturnType)
        {
            
        }
    }


    private static readonly ImmutableArray<IntrinsicDefinition> Definitions =
    [
        new(Intrinsic.NotBool, TokenKind.Bang, PrimitiveKind.Bool, [], PrimitiveKind.Bool),
        new(Intrinsic.EqualsBool, TokenKind.DoubleEqual, PrimitiveKind.Bool, [PrimitiveKind.Bool], PrimitiveKind.Bool),
        new(Intrinsic.ToStringBool, Names.ToStringMethod, PrimitiveKind.Bool, [], PrimitiveKind.String),

        new(Intrinsic.AddI32, TokenKind.Plus, PrimitiveKind.I32, [PrimitiveKind.I32], PrimitiveKind.I32),
        new(Intrinsic.SubtractI32, TokenKind.Minus, PrimitiveKind.I32, [PrimitiveKind.I32], PrimitiveKind.I32),
        new(Intrinsic.NegateI32, TokenKind.Minus, PrimitiveKind.I32, [], PrimitiveKind.I32),
        new(Intrinsic.MultiplyI32, TokenKind.Star, PrimitiveKind.I32, [PrimitiveKind.I32], PrimitiveKind.I32),
        new(Intrinsic.DivideI32, TokenKind.Slash, PrimitiveKind.I32, [PrimitiveKind.I32], PrimitiveKind.I32),
        new(Intrinsic.LessThanI32, TokenKind.LessThan, PrimitiveKind.I32, [PrimitiveKind.I32], PrimitiveKind.Bool),
        new(Intrinsic.EqualsI32, TokenKind.DoubleEqual, PrimitiveKind.I32, [PrimitiveKind.I32], PrimitiveKind.Bool),
        new(Intrinsic.ToStringI32, Names.ToStringMethod, PrimitiveKind.I32, [], PrimitiveKind.String),
        
        new(Intrinsic.AddI64, TokenKind.Plus, PrimitiveKind.I64, [PrimitiveKind.I64], PrimitiveKind.I64),
        new(Intrinsic.SubtractI64, TokenKind.Minus, PrimitiveKind.I64, [PrimitiveKind.I64], PrimitiveKind.I64),
        new(Intrinsic.NegateI64, TokenKind.Minus, PrimitiveKind.I64, [], PrimitiveKind.I64),
        new(Intrinsic.MultiplyI64, TokenKind.Star, PrimitiveKind.I64, [PrimitiveKind.I64], PrimitiveKind.I64),
        new(Intrinsic.DivideI64, TokenKind.Slash, PrimitiveKind.I64, [PrimitiveKind.I64], PrimitiveKind.I64),
        new(Intrinsic.LessThanI64, TokenKind.LessThan, PrimitiveKind.I64, [PrimitiveKind.I64], PrimitiveKind.Bool),
        new(Intrinsic.EqualsI64, TokenKind.DoubleEqual, PrimitiveKind.I64, [PrimitiveKind.I64], PrimitiveKind.Bool),
        new(Intrinsic.ToStringI64, Names.ToStringMethod, PrimitiveKind.I64, [], PrimitiveKind.String),
        
        new(Intrinsic.AddF32, TokenKind.Plus, PrimitiveKind.F32, [PrimitiveKind.F32], PrimitiveKind.F32),
        new(Intrinsic.SubtractF32, TokenKind.Minus, PrimitiveKind.F32, [PrimitiveKind.F32], PrimitiveKind.F32),
        new(Intrinsic.NegateF32, TokenKind.Minus, PrimitiveKind.F32, [], PrimitiveKind.F32),
        new(Intrinsic.MultiplyF32, TokenKind.Star, PrimitiveKind.F32, [PrimitiveKind.F32], PrimitiveKind.F32),
        new(Intrinsic.DivideF32, TokenKind.Slash, PrimitiveKind.F32, [PrimitiveKind.F32], PrimitiveKind.F32),
        new(Intrinsic.LessThanF32, TokenKind.LessThan, PrimitiveKind.F32, [PrimitiveKind.F32], PrimitiveKind.Bool),
        new(Intrinsic.EqualsF32, TokenKind.DoubleEqual, PrimitiveKind.F32, [PrimitiveKind.F32], PrimitiveKind.Bool),
        new(Intrinsic.ToStringF32, Names.ToStringMethod, PrimitiveKind.F32, [], PrimitiveKind.String),
        
        new(Intrinsic.AddF64, TokenKind.Plus, PrimitiveKind.F64, [PrimitiveKind.F64], PrimitiveKind.F64),
        new(Intrinsic.SubtractF64, TokenKind.Minus, PrimitiveKind.F64, [PrimitiveKind.F64], PrimitiveKind.F64),
        new(Intrinsic.NegateF64, TokenKind.Minus, PrimitiveKind.F64, [], PrimitiveKind.F64),
        new(Intrinsic.MultiplyF64, TokenKind.Star, PrimitiveKind.F64, [PrimitiveKind.F64], PrimitiveKind.F64),
        new(Intrinsic.DivideF64, TokenKind.Slash, PrimitiveKind.F64, [PrimitiveKind.F64], PrimitiveKind.F64),
        new(Intrinsic.LessThanF64, TokenKind.LessThan, PrimitiveKind.F64, [PrimitiveKind.F64], PrimitiveKind.Bool),
        new(Intrinsic.EqualsF64, TokenKind.DoubleEqual, PrimitiveKind.F64, [PrimitiveKind.F64], PrimitiveKind.Bool),
        new(Intrinsic.ToStringF64, Names.ToStringMethod, PrimitiveKind.F64, [], PrimitiveKind.String),
        
        new(Intrinsic.EqualsString, TokenKind.DoubleEqual, PrimitiveKind.String, [PrimitiveKind.String], PrimitiveKind.Bool),
        
        new(Intrinsic.Print, "Print", [PrimitiveKind.String]),
    ];
    
    public static Intrinsic? TryGetIntrinsic(string name, PrimitiveKind? receiver,
        ImmutableArray<PrimitiveKind> arguments, PrimitiveKind? returnType)
    {
        var definition = Definitions.FirstOrDefault(d => d.Name == name && d.Receiver == receiver &&
                                                         d.Parameters.SequenceEqual(arguments) && d.ReturnType == returnType);
        if (definition is null) return null;
        return definition.Intrinsic;
    }
}