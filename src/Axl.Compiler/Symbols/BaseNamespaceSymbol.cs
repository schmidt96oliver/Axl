using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Symbols;

public sealed class BaseNamespaceSymbol : NamespaceSymbol
{
    public override ImmutableArray<Symbol> Members { get; }

    public StructSymbol I32 { get; }
    public StructSymbol I64 { get; }
    public StructSymbol F32 { get; }
    public StructSymbol F64 { get; }
    public StructSymbol String { get; }
    public StructSymbol Bool { get; }
    public StructSymbol Unit { get; }

    public StructSymbol DefaultIntType => I32;
    public StructSymbol DefaultFloatType => F64;


    public BaseNamespaceSymbol(Symbol owner)
        : base("Base", owner)
    {
        //TODO: String should not be a struct, probably :D
        
        Bool = new StructSymbol("Bool", this, isPublic: true, isPrimitive: true);
        String = new StructSymbol("String", this, isPublic: true, isPrimitive: true);
        I32 = new StructSymbol("I32", this, isPublic: true, isPrimitive: true);
        I64 = new StructSymbol("I64", this, isPublic: true, isPrimitive: true);
        F32 = new StructSymbol("F32", this, isPublic: true, isPrimitive: true);
        F64 = new StructSymbol("F64", this, isPublic: true, isPrimitive: true);
        Unit = new StructSymbol("Unit", this, isPublic: true, isPrimitive: false);
        
        // Bool members need to be generated first, because
        // generation will look for ! operator.
        Bool.SetMembers(AddRemainingMembers(GetBoolMembers()));
        
        I32.SetMembers(AddRemainingMembers(GetI32Members()));
        I64.SetMembers(AddRemainingMembers(GetI64Members()));
        F32.SetMembers(AddRemainingMembers(GetF32Members()));
        F64.SetMembers(AddRemainingMembers(GetF64Members()));
        String.SetMembers(AddRemainingMembers(GetStringMembers()));
        Unit.SetMembers(AddRemainingMembers(GetUnitMembers()));

        var funs = GeneratePrintFuns(I32, I64, F32, F64, Bool);
        Members = [I32, I64, F32, F64, Bool, String, Unit, .. funs];
    }


    private ImmutableArray<Symbol> GeneratePrintFuns(params ReadOnlySpan<TypeSymbol> types)
    {
        var printFuns = ImmutableArray.CreateBuilder<FunSymbol>();
        var printLineFuns = ImmutableArray.CreateBuilder<FunSymbol>();
        
        var print = new FunSymbol("Print", 
            owner: this,
            isPublic: true,
            receiverType: null, 
            returnType: Unit, 
            body: Intrinsic.Print);
        print.SetParameters([new ParameterSymbol("text", print, String)]);
        printFuns.Add(print);

        // PrintLine = Print("{arg}\n")
        var b = FunBuilder.Static("PrintLine", this, this);
        b.Param("text", String);
        b.StaticCall(print, b.String(b.Arg0, "\n"));
        var printLine = b.ToFun();
        printLineFuns.Add(printLine);
        
        // PrintLine() = Print("\n")
        b = FunBuilder.Static("PrintLine", this, this);
        b.StaticCall(print, b.String("\n"));
        printLineFuns.Add(b.ToFun());

        // Generate Print(T) and PrintLine(T) for all 
        // T that have T.ToString()
        foreach (var type in types)
        {
            if (type.Members.OfType<FunSymbol>().FirstOrDefault(fun =>
                    fun.Name is "ToString" && fun.ReceiverType == type && fun.Parameters.Length == 0)
                is not { } toString)
            {
                continue;
            }

            var typePrint = FunBuilder.Static("Print", this, this);
            typePrint.Param("arg", type);
            typePrint.StaticCall(print, typePrint.InstanceCall(toString, typePrint.Arg0));
            printFuns.Add(typePrint.ToFun());
            
            var typePrintLine = FunBuilder.Static("PrintLine", this, this);
            typePrintLine.Param("arg", type);
            typePrintLine.StaticCall(printLine, typePrintLine.InstanceCall(toString, typePrintLine.Arg0));
            printLineFuns.Add(typePrintLine.ToFun());
        }

        var printGroup = new FunGroupSymbol("Print", this, printFuns.DrainToImmutable());
        var printLineGroup = new FunGroupSymbol("PrintLine", this, printLineFuns.DrainToImmutable());
        return [printGroup, printLineGroup];
    }

    private FunSymbol IntrinsicMethodNoParam(string name, TypeSymbol type, TypeSymbol returnType,
        Intrinsic intrinsic)
    {
        var fun = new FunSymbol(name, owner: type, isPublic: true, receiverType: type,
            returnType, body: intrinsic);
        fun.SetParameters([]);
        return fun;
    }
    
    private FunSymbol IntrinsicMethodNoParam(TokenKind tokenName, TypeSymbol type, TypeSymbol returnType,
        Intrinsic intrinsic)
    {
        return IntrinsicMethodNoParam(SyntaxFacts.GetText(tokenName)!, type, returnType, intrinsic);
    }
    private FunSymbol IntrinsicMethodSingleParam(string name, TypeSymbol type, TypeSymbol returnType,
        Intrinsic intrinsic)
    {
        var fun = new FunSymbol(name, owner: type, isPublic: true, receiverType: type,
            returnType, body: intrinsic);
        fun.SetParameters([new ParameterSymbol("right", fun, type)]);
        return fun;
    }
    
    private FunSymbol IntrinsicMethodSingleParam(TokenKind tokenName, TypeSymbol type, TypeSymbol returnType,
        Intrinsic intrinsic)
    {
        return IntrinsicMethodSingleParam(SyntaxFacts.GetText(tokenName)!, type, returnType, intrinsic);
    }
    
    
    private ImmutableArray<Symbol> GetBoolMembers() =>
    [
        IntrinsicMethodNoParam(TokenKind.Bang, Bool, returnType: Bool, Intrinsic.NotBool),
        IntrinsicMethodSingleParam(TokenKind.DoubleEqual, Bool, returnType: Bool, Intrinsic.EqualsBool),

        IntrinsicMethodNoParam("ToString", Bool, returnType: String, Intrinsic.ToStringBool),
    ];

    private ImmutableArray<Symbol> GetUnitMembers()
    {
        var equal = FunBuilder.Method(TokenKind.DoubleEqual, Unit, receiverType: Unit,
            this, returnType: Bool);
        equal.Param("right", Unit);
        equal.Return(equal.True());
        return [equal.ToFun()];
    }

    private ImmutableArray<Symbol> GetStringMembers() =>
    [
        IntrinsicMethodSingleParam(TokenKind.DoubleEqual, String, returnType: Bool, Intrinsic.EqualsString),
    ];

    private ImmutableArray<Symbol> GetI32Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, I32, [
            IntrinsicMethodNoParam(TokenKind.Minus, I32, returnType: I32, Intrinsic.NegateI32),
            IntrinsicMethodSingleParam(TokenKind.Minus, I32, returnType: I32, Intrinsic.SubtractI32),
        ]),

        IntrinsicMethodSingleParam(TokenKind.Plus, I32,
            returnType: I32, Intrinsic.AddI32),

        IntrinsicMethodSingleParam(TokenKind.Star, I32,
            returnType: I32, Intrinsic.MultiplyI32),
        IntrinsicMethodSingleParam(TokenKind.Slash, I32,
            returnType: I32, Intrinsic.DivideI32),

        IntrinsicMethodSingleParam(TokenKind.DoubleEqual, I32,
            returnType: Bool, Intrinsic.EqualsI32),
        IntrinsicMethodSingleParam(TokenKind.LessThan, I32,
            returnType: Bool, Intrinsic.LessThanI32),

        IntrinsicMethodNoParam("ToString", I32, returnType: String, Intrinsic.ToStringI32),
    ];

    private ImmutableArray<Symbol> GetI64Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, I64, [
            IntrinsicMethodNoParam(TokenKind.Minus, I64, returnType: I64, Intrinsic.NegateI64),
            IntrinsicMethodSingleParam(TokenKind.Minus, I64,
                returnType: I64, Intrinsic.SubtractI64),
        ]),

        IntrinsicMethodSingleParam(TokenKind.Plus, I64,
            returnType: I64, Intrinsic.AddI64),
        IntrinsicMethodSingleParam(TokenKind.Star, I64,
            returnType: I64, Intrinsic.MultiplyI64),
        IntrinsicMethodSingleParam(TokenKind.Slash, I64,
            returnType: I64, Intrinsic.DivideI64),

        IntrinsicMethodSingleParam(TokenKind.DoubleEqual, I64,
            returnType: Bool, Intrinsic.EqualsI64),
        IntrinsicMethodSingleParam(TokenKind.LessThan, I64,
            returnType: Bool, Intrinsic.LessThanI64),

        IntrinsicMethodNoParam("ToString", I64, returnType: String, Intrinsic.ToStringI64),
    ];

    private ImmutableArray<Symbol> GetF32Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, F32, [
            IntrinsicMethodNoParam(TokenKind.Minus, F32, returnType: F32, Intrinsic.NegateF32),
            IntrinsicMethodSingleParam(TokenKind.Minus,
                F32, returnType: F32, Intrinsic.SubtractF32),
        ]),

        IntrinsicMethodSingleParam(TokenKind.Plus,
            F32, returnType: F32, Intrinsic.AddF32),
        IntrinsicMethodSingleParam(TokenKind.Star,
            F32, returnType: F32, Intrinsic.MultiplyF32),
        IntrinsicMethodSingleParam(TokenKind.Slash,
            F32, returnType: F32, Intrinsic.DivideF32),

        IntrinsicMethodSingleParam(TokenKind.DoubleEqual,
            F32, returnType: Bool, Intrinsic.EqualsF32),
        IntrinsicMethodSingleParam(TokenKind.LessThan,
            F32, returnType: Bool, Intrinsic.LessThanF32),

        IntrinsicMethodNoParam("ToString", F32, returnType: String, Intrinsic.ToStringF32),
    ];

    private ImmutableArray<Symbol> GetF64Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, F64, [
            IntrinsicMethodNoParam(TokenKind.Minus, F64, returnType: F64, Intrinsic.NegateF64),
            IntrinsicMethodSingleParam(TokenKind.Minus, F64,
                returnType: F64, Intrinsic.SubtractF64),
        ]),

        IntrinsicMethodSingleParam(TokenKind.Plus, F64,
            returnType: F64, Intrinsic.AddF64),
        IntrinsicMethodSingleParam(TokenKind.Star, F64,
            returnType: F64, Intrinsic.MultiplyF64),
        IntrinsicMethodSingleParam(TokenKind.Slash, F64,
            returnType: F64, Intrinsic.DivideF64),

        IntrinsicMethodSingleParam(TokenKind.DoubleEqual, F64,
            returnType: Bool, Intrinsic.EqualsF64),
        IntrinsicMethodSingleParam(TokenKind.LessThan, F64,
            returnType: Bool, Intrinsic.LessThanF64),

        IntrinsicMethodNoParam("ToString", F64, returnType: String, Intrinsic.ToStringF64),
    ];


    private ImmutableArray<Symbol> AddRemainingMembers(ImmutableArray<Symbol> members)
    {
        // Search in members first, because we might be adding to the bool type.
        var not = members.OfType<FunSymbol>().FirstOrDefault(fun =>
                      fun.Name == SyntaxFacts.GetText(TokenKind.Bang) && fun.Parameters.Length == 0 &&
                      fun.ReceiverType == Bool)
                  ?? Bool.Members.OfType<FunSymbol>().First(fun =>
                      fun.Name == SyntaxFacts.GetText(TokenKind.Bang) && fun.Parameters.Length == 0 &&
                      fun.ReceiverType == Bool);

        var equal = members.OfType<FunSymbol>()
            .First(fun =>
                fun.Name == SyntaxFacts.GetText(TokenKind.DoubleEqual) && fun.Parameters.Length == 1 &&
                fun.ReceiverType is not null);

        var notEqual = GenerateNotEqual(equal, not);

        if (members.OfType<FunSymbol>()
                .FirstOrDefault(fun =>
                    fun.Name == SyntaxFacts.GetText(TokenKind.LessThan) && fun.Parameters.Length == 1 &&
                    fun.ReceiverType is not null) is FunSymbol lessThan)
        {
            var greaterThan = GenerateGreaterThan(lessThan);
            var lessThanOrEqual = GenerateComparisonOrEqual(lessThan, equal, TokenKind.LessThanEqual);
            var greaterThanOrEqual = GenerateComparisonOrEqual(greaterThan, equal, TokenKind.GreaterThanEqual);

            return [.. members, notEqual, greaterThan, greaterThanOrEqual, lessThanOrEqual];
        }

        return [.. members, notEqual];
    }

    private FunSymbol GenerateNotEqual(FunSymbol equal, FunSymbol notFun)
    {
        Debug.Assert(equal.ReceiverType is not null);

        var b = FunBuilder.Method(TokenKind.BangEqual, equal.Owner!, equal.ReceiverType, this, returnType: Bool);
        b.Param("right", equal.ParameterTypes[0]);
        b.Return(b.InstanceCall(notFun, b.InstanceCall(equal, b.Self, b.Arg0)));
        return b.ToFun();
    }

    private FunSymbol GenerateGreaterThan(FunSymbol lessThan)
    {
        var b = FunBuilder.Method(TokenKind.GreaterThan, lessThan.Owner!, lessThan.ParameterTypes[0], this, returnType: Bool);
        b.Param("right", lessThan.ReceiverType!);
        b.Return(b.InstanceCall(lessThan, b.Arg0, b.Self));
        return b.ToFun();
    }

    private FunSymbol GenerateComparisonOrEqual(FunSymbol comparison, FunSymbol equal, TokenKind name)
    {
        // self <= right = self < right || self == right
        // self >= right = self > right || self == right
        
        Debug.Assert(comparison.ReceiverType is not null);
        Debug.Assert(comparison.ReceiverType == equal.ReceiverType
                     && comparison.ParameterTypes.SequenceEqual(equal.ParameterTypes));
        
        var b = FunBuilder.Method(name, comparison.Owner!, comparison.ReceiverType, this,
            returnType: Bool);
        b.Param("right", comparison.ParameterTypes[0]);
        b.Return(b.Or(b.InstanceCall(comparison, b.Self, b.Arg0), b.InstanceCall(equal, b.Self, b.Arg0)));
        return b.ToFun();
    }
}