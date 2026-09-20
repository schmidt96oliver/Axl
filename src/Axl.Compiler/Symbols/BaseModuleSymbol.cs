using System.Collections.Immutable;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Symbols;

public sealed class BaseModuleSymbol : ModuleOrTypeSymbol
{
    public override SymbolKind Kind => SymbolKind.Module;

    public override ImmutableArray<Symbol> Members { get; }
    
    public TypeSymbol I32 { get; }
    public TypeSymbol I64 { get;}
    public TypeSymbol F32 { get; }
    public TypeSymbol F64 { get; }
    public TypeSymbol String { get; }
    public TypeSymbol Bool { get; }
    public TypeSymbol Unit { get; }
    
    public TypeSymbol Never { get; }
    public TypeSymbol Error { get; }


    public TypeSymbol DefaultIntType => I32;
    public TypeSymbol DefaultFloatType => F64;
    
    
    public BaseModuleSymbol()
        : base("Base")
    {
        Bool = new TypeSymbol("Bool", GetBoolMembers);
        
        I32 = new TypeSymbol("I32", GetI32Members);
        I64 = new TypeSymbol("I64", GetI64Members);
        F32 = new TypeSymbol("F32", GetF32Members);
        F64 = new TypeSymbol("F64", GetF64Members);
        String = new TypeSymbol("String", GetStringMembers);
        Unit = new TypeSymbol("Unit", GetUnitMembers);
        
        Members = [I32, I64, F32, F64, Bool, String, Unit];
        
        // Error and Never are not nameable from code, so they will not become
        // members.
        Error = new TypeSymbol("Error", () => []);
        Never = new TypeSymbol("Never", () => []);
    }

    
    
    private ImmutableArray<Symbol> GetBoolMembers() =>
    [
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.NotKw)!, Intrinsic.NotBool, [Bool], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, Intrinsic.EqualsBool, [Bool, Bool], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, Intrinsic.NotEqualsBool, [Bool, Bool], Bool),
        
        new IntrinsicFunSymbol("ToString", Intrinsic.ToStringBool, [Bool], String),
    ];

    private ImmutableArray<Symbol> GetUnitMembers() =>
    [
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, Intrinsic.EqualsUnit, [Unit, Unit], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, Intrinsic.NotEqualsUnit, [Unit, Unit], Bool),
    ];
    private ImmutableArray<Symbol> GetStringMembers() =>
    [
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, Intrinsic.EqualsString, [String, String], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, Intrinsic.NotEqualsString, [String, String], Bool)
    ];
    
    private ImmutableArray<Symbol> GetI32Members() =>
    [
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.NegateI32, [I32], I32),

        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, Intrinsic.AddI32, [I32, I32], I32),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.SubtractI32, [I32, I32], I32),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, Intrinsic.MultiplyI32, [I32, I32], I32),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, Intrinsic.DivideI32, [I32, I32], I32),

        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, Intrinsic.EqualsI32, [I32, I32], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, Intrinsic.NotEqualsI32, [I32, I32], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, Intrinsic.LessThanI32, [I32, I32], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, Intrinsic.LessThanOrEqualI32, [I32, I32], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, Intrinsic.GreaterThanI32, [I32, I32], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!, Intrinsic.GreaterThanOrEqualI32, [I32, I32], Bool),
        
        new IntrinsicFunSymbol("ToString", Intrinsic.ToStringI32, [I32], String),
    ];
    
    private ImmutableArray<Symbol> GetI64Members() =>
    [
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.NegateI64, [I64], I64),

        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, Intrinsic.AddI64, [I64, I64], I64),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.SubtractI64, [I64, I64], I64),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, Intrinsic.MultiplyI64, [I64, I64], I64),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, Intrinsic.DivideI64, [I64, I64], I64),

        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, Intrinsic.EqualsI64, [I64, I64], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, Intrinsic.NotEqualsI64, [I64, I64], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, Intrinsic.LessThanI64, [I64, I64], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, Intrinsic.LessThanOrEqualI64, [I64, I64], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, Intrinsic.GreaterThanI64, [I64, I64], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!, Intrinsic.GreaterThanOrEqualI64, [I64, I64], Bool),
        
        new IntrinsicFunSymbol("ToString", Intrinsic.ToStringI64, [I64], String),
    ];
    
    private ImmutableArray<Symbol> GetF32Members() =>
    [
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.NegateF32, [F32], F32),

        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, Intrinsic.AddF32, [F32, F32], F32),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.SubtractF32, [F32, F32], F32),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, Intrinsic.MultiplyF32, [F32, F32], F32),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, Intrinsic.DivideF32, [F32, F32], F32),
        
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, Intrinsic.EqualsF32, [F32, F32], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, Intrinsic.NotEqualsF32, [F32, F32], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, Intrinsic.LessThanF32, [F32, F32], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, Intrinsic.LessThanOrEqualF32, [F32, F32], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, Intrinsic.GreaterThanF32, [F32, F32], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!, Intrinsic.GreaterThanOrEqualF32, [F32, F32], Bool),
        
        new IntrinsicFunSymbol("ToString", Intrinsic.ToStringF32, [F32], String),
    ];
    
    private ImmutableArray<Symbol> GetF64Members() =>
    [
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.NegateF64, [F64], F64),

        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, Intrinsic.AddF64, [F64, F64], F64),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.SubtractF64, [F64, F64], F64),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, Intrinsic.MultiplyF64, [F64, F64], F64),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, Intrinsic.DivideF64, [F64, F64], F64),

        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, Intrinsic.EqualsF64, [F64, F64], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, Intrinsic.NotEqualsF64, [F64, F64], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, Intrinsic.LessThanF64, [F64, F64], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, Intrinsic.LessThanOrEqualF64, [F64, F64], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, Intrinsic.GreaterThanF64, [F64, F64], Bool),
        new IntrinsicFunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!, Intrinsic.GreaterThanOrEqualF64, [F64, F64], Bool),
        
        new IntrinsicFunSymbol("ToString", Intrinsic.ToStringF64, [F64], String),
    ];



}