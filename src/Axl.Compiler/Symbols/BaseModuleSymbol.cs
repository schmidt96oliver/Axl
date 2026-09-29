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

        var funs = GetFuns();
        
        Members = [I32, I64, F32, F64, Bool, String, Unit, ..funs];
        
        // Error and Never are not nameable from code, so they will not become
        // members.
        Error = new TypeSymbol("Error", () => []);
        Never = new TypeSymbol("Never", () => []);
    }


    private ImmutableArray<Symbol> GetFuns() =>
    [
        new FunSymbol("Print", Intrinsic.Print, receiver: null, [String], Unit)
    ];
    
    
    private ImmutableArray<Symbol> GetBoolMembers() =>
    [
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Bang)!, Intrinsic.NotBool, receiver: Bool, [], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, Intrinsic.EqualsBool, receiver: Bool, [Bool], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, Intrinsic.NotEqualsBool, receiver: Bool, [Bool], Bool),
        
        new FunSymbol("ToString", Intrinsic.ToStringBool, receiver: Bool, [], String),
    ];

    private ImmutableArray<Symbol> GetUnitMembers() =>
    [
        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, Intrinsic.EqualsUnit, receiver: Unit, [Unit], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, Intrinsic.NotEqualsUnit, receiver: Unit, [Unit], Bool),
    ];
    private ImmutableArray<Symbol> GetStringMembers() =>
    [
        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, Intrinsic.EqualsString, receiver: String, [String], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, Intrinsic.NotEqualsString, receiver: String, [String], Bool)
    ];

    private ImmutableArray<Symbol> GetI32Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, [
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.NegateI32, receiver: I32, [], I32),
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.SubtractI32, receiver: I32, [I32], I32),
        ]),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, Intrinsic.AddI32, receiver: I32, [I32], I32),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, Intrinsic.MultiplyI32, receiver: I32, [I32], I32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, Intrinsic.DivideI32, receiver: I32, [I32], I32),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, Intrinsic.EqualsI32, receiver: I32, [I32], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, Intrinsic.NotEqualsI32, receiver: I32, [I32], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, Intrinsic.LessThanI32, receiver: I32, [I32], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, Intrinsic.LessThanOrEqualI32, receiver: I32, [I32],
            Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, Intrinsic.GreaterThanI32, receiver: I32, [I32],
            Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!, Intrinsic.GreaterThanOrEqualI32, receiver: I32,
            [I32], Bool),

        new FunSymbol("ToString", Intrinsic.ToStringI32, receiver: I32, [], String),
    ];
    
    private ImmutableArray<Symbol> GetI64Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, [
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.NegateI64, receiver: I64, [], I64),
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.SubtractI64, receiver: I64, [I64], I64),
        ]),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, Intrinsic.AddI64, receiver: I64, [I64], I64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, Intrinsic.MultiplyI64, receiver: I64, [I64], I64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, Intrinsic.DivideI64, receiver: I64, [I64], I64),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, Intrinsic.EqualsI64, receiver: I64, [I64], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, Intrinsic.NotEqualsI64, receiver: I64, [I64], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, Intrinsic.LessThanI64, receiver: I64, [I64], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, Intrinsic.LessThanOrEqualI64, receiver: I64, [I64], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, Intrinsic.GreaterThanI64, receiver: I64, [I64], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!, Intrinsic.GreaterThanOrEqualI64, receiver: I64, [I64], Bool),
        
        new FunSymbol("ToString", Intrinsic.ToStringI64, receiver: I64, [], String),
    ];
    
    private ImmutableArray<Symbol> GetF32Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, [
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.NegateF32, receiver: F32, [], F32),
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.SubtractF32, receiver: F32, [F32], F32),
        ]),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, Intrinsic.AddF32, receiver: F32, [F32], F32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, Intrinsic.MultiplyF32, receiver: F32, [F32], F32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, Intrinsic.DivideF32, receiver: F32, [F32], F32),
        
        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, Intrinsic.EqualsF32, receiver: F32, [F32], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, Intrinsic.NotEqualsF32, receiver: F32, [F32], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, Intrinsic.LessThanF32, receiver: F32, [F32], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, Intrinsic.LessThanOrEqualF32, receiver: F32, [F32], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, Intrinsic.GreaterThanF32, receiver: F32, [F32], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!, Intrinsic.GreaterThanOrEqualF32, receiver: F32, [F32], Bool),
        
        new FunSymbol("ToString", Intrinsic.ToStringF32, receiver: F32, [], String),
    ];
    
    private ImmutableArray<Symbol> GetF64Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, [
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.NegateF64, receiver: F64, [], F64),
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, Intrinsic.SubtractF64, receiver: F64, [F64], F64),
        ]),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, Intrinsic.AddF64, receiver: F64, [F64], F64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, Intrinsic.MultiplyF64, receiver: F64, [F64], F64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, Intrinsic.DivideF64, receiver: F64, [F64], F64),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, Intrinsic.EqualsF64, receiver: F64, [F64], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, Intrinsic.NotEqualsF64, receiver: F64, [F64], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, Intrinsic.LessThanF64, receiver: F64, [F64], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, Intrinsic.LessThanOrEqualF64, receiver: F64, [F64], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, Intrinsic.GreaterThanF64, receiver: F64, [F64], Bool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!, Intrinsic.GreaterThanOrEqualF64, receiver: F64, [F64], Bool),
        
        new FunSymbol("ToString", Intrinsic.ToStringF64, receiver: F64, [], String),
    ];
}