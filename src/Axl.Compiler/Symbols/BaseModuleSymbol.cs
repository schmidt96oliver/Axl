using System.Collections.Immutable;

namespace Axl.Compiler.Symbols;

public sealed class BaseModuleSymbol : Symbol
{
    public override string KindName => "module";
    
    public ImmutableArray<Symbol> Members { get; }
    
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
        : base(name: SymbolName.From("Base"))
    {
        Bool = new TypeSymbol("Bool", GetBoolMembers);
        
        I32 = new TypeSymbol("I32", i32 => GetI32Members(i32, Bool));
        I64 = new TypeSymbol("I64", i64 => GetI64Members(i64, Bool));
        F32 = new TypeSymbol("F32", f32 => GetF32Members(f32, Bool));
        F64 = new TypeSymbol("F64", f64 => GetF64Members(f64, Bool));
        String = new TypeSymbol("String", _ => []);
        Unit = new TypeSymbol("Unit", _ => []);
        
        Members = [I32, I64, F32, F64, Bool, String, Unit];
        
        // Error and Never are not nameable from code, so they will not become
        // members.
        Error = new TypeSymbol("Error", _ => []);
        Never = new TypeSymbol("Never", _ => []);
    }


    public ImmutableArray<Symbol> LookupMember(SymbolName name)
        => name.IsEmpty
            ? []
            : [.. Members.Where(symbol => symbol.Name == name)];
    
    
    private static ImmutableArray<Symbol> GetBoolMembers(TypeSymbol @bool) =>
    [
        new IntrinsicFunSymbol(SymbolName.From("not"), Intrinsic.NotBool, [@bool], @bool),
    ];
    
    private static ImmutableArray<Symbol> GetI32Members(TypeSymbol i32, TypeSymbol @bool) =>
    [
        new IntrinsicFunSymbol(SymbolName.From("-"), Intrinsic.NegateI32, [i32], i32),

        new IntrinsicFunSymbol(SymbolName.From("+"), Intrinsic.AddI32, [i32, i32], i32),
        new IntrinsicFunSymbol(SymbolName.From("-"), Intrinsic.SubtractI32, [i32, i32], i32),
        new IntrinsicFunSymbol(SymbolName.From("*"), Intrinsic.MultiplyI32, [i32, i32], i32),
        new IntrinsicFunSymbol(SymbolName.From("/"), Intrinsic.DivideI32, [i32, i32], i32),

        new IntrinsicFunSymbol(SymbolName.From("=="), Intrinsic.EqualsI32, [i32, i32], @bool),
        new IntrinsicFunSymbol(SymbolName.From("<"), Intrinsic.LessThanI32, [i32, i32], @bool),
    ];
    
    private static ImmutableArray<Symbol> GetI64Members(TypeSymbol i64, TypeSymbol @bool) =>
    [
        new IntrinsicFunSymbol(SymbolName.From("-"), Intrinsic.NegateI64, [i64], i64),

        new IntrinsicFunSymbol(SymbolName.From("+"), Intrinsic.AddI64, [i64, i64], i64),
        new IntrinsicFunSymbol(SymbolName.From("-"), Intrinsic.SubtractI64, [i64, i64], i64),
        new IntrinsicFunSymbol(SymbolName.From("*"), Intrinsic.MultiplyI64, [i64, i64], i64),
        new IntrinsicFunSymbol(SymbolName.From("/"), Intrinsic.DivideI64, [i64, i64], i64),

        new IntrinsicFunSymbol(SymbolName.From("=="), Intrinsic.EqualsI64, [i64, i64], @bool),
        new IntrinsicFunSymbol(SymbolName.From("<"), Intrinsic.LessThanI64, [i64, i64], @bool),
    ];
    
    private static ImmutableArray<Symbol> GetF32Members(TypeSymbol f32, TypeSymbol @bool) =>
    [
        new IntrinsicFunSymbol(SymbolName.From("-"), Intrinsic.NegateF32, [f32], f32),

        new IntrinsicFunSymbol(SymbolName.From("+"), Intrinsic.AddF32, [f32, f32], f32),
        new IntrinsicFunSymbol(SymbolName.From("-"), Intrinsic.SubtractF32, [f32, f32], f32),
        new IntrinsicFunSymbol(SymbolName.From("*"), Intrinsic.MultiplyF32, [f32, f32], f32),
        new IntrinsicFunSymbol(SymbolName.From("/"), Intrinsic.DivideF32, [f32, f32], f32),

        new IntrinsicFunSymbol(SymbolName.From("=="), Intrinsic.EqualsF32, [f32, f32], @bool),
        new IntrinsicFunSymbol(SymbolName.From("<"), Intrinsic.LessThanF32, [f32, f32], @bool),
    ];
    
    private static ImmutableArray<Symbol> GetF64Members(TypeSymbol f64, TypeSymbol @bool) =>
    [
        new IntrinsicFunSymbol(SymbolName.From("-"), Intrinsic.NegateF64, [f64], f64),

        new IntrinsicFunSymbol(SymbolName.From("+"), Intrinsic.AddF64, [f64, f64], f64),
        new IntrinsicFunSymbol(SymbolName.From("-"), Intrinsic.SubtractF64, [f64, f64], f64),
        new IntrinsicFunSymbol(SymbolName.From("*"), Intrinsic.MultiplyF64, [f64, f64], f64),
        new IntrinsicFunSymbol(SymbolName.From("/"), Intrinsic.DivideF64, [f64, f64], f64),

        new IntrinsicFunSymbol(SymbolName.From("=="), Intrinsic.EqualsF64, [f64, f64], @bool),
        new IntrinsicFunSymbol(SymbolName.From("<"), Intrinsic.LessThanF64, [f64, f64], @bool),
    ];



}