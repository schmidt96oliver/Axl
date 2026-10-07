using System.Collections.Immutable;
using System.Diagnostics;
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


    public BaseNamespaceSymbol()
        : base("Base")
    {
        //TODO: String should not be a struct, probably :D
        
        Bool = new StructSymbol("Bool", isPrimitive: true);
        String = new StructSymbol("String", isPrimitive: true);
        I32 = new StructSymbol("I32", isPrimitive: true);
        I64 = new StructSymbol("I64", isPrimitive: true);
        F32 = new StructSymbol("F32", isPrimitive: true);
        F64 = new StructSymbol("F64", isPrimitive: true);
        Unit = new StructSymbol("Unit", isPrimitive: false);
        
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
            receiverType: null, 
            parameters: [new ParameterSymbol("text", String)],
            returnType: Unit, 
            body: Intrinsic.Print);
        printFuns.Add(print);

        // PrintLine = Print("{arg}\n")
        var b = new FunBuilder(this);
        b.Param("text", String);
        b.StaticCall(print, b.String(b.Arg0, "\n"));
        var printLine = b.ToFun("PrintLine");
        printLineFuns.Add(printLine);
        
        // PrintLine() = Print("\n")
        b = new FunBuilder(this);
        b.StaticCall(print, b.String("\n"));
        printLineFuns.Add(b.ToFun("PrintLine"));

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

            var typePrint = new FunBuilder(this);
            typePrint.Param("arg", type);
            typePrint.StaticCall(print, typePrint.InstanceCall(toString, typePrint.Arg0));
            printFuns.Add(typePrint.ToFun("Print"));
            
            var typePrintLine = new FunBuilder(this);
            typePrintLine.Param("arg", type);
            typePrintLine.StaticCall(printLine, typePrintLine.InstanceCall(toString, typePrintLine.Arg0));
            printLineFuns.Add(typePrintLine.ToFun("PrintLine"));
        }

        var printGroup = new FunGroupSymbol("Print", printFuns.DrainToImmutable());
        var printLineGroup = new FunGroupSymbol("PrintLine", printLineFuns.DrainToImmutable());
        return [printGroup, printLineGroup];
    }


    private ImmutableArray<Symbol> GetBoolMembers() =>
    [
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Bang)!, receiverType: Bool, parameters: [], returnType: Bool, body: Intrinsic.NotBool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: Bool,
            parameters: [new ParameterSymbol("right", Bool)], returnType: Bool, body: Intrinsic.EqualsBool),

        new FunSymbol("ToString", receiverType: Bool, parameters: [], returnType: String, body: Intrinsic.ToStringBool),
    ];

    private ImmutableArray<Symbol> GetUnitMembers()
    {
        var equal = new FunBuilder(this);
        equal.Receiver(Unit);
        equal.Param("right", Unit);
        equal.Return(equal.True());
        return [equal.ToFun(TokenKind.DoubleEqual)];
    }

    private ImmutableArray<Symbol> GetStringMembers() =>
    [
        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: String,
            parameters: [new ParameterSymbol("right", String)], returnType: Bool, body: Intrinsic.EqualsString),
    ];

    private ImmutableArray<Symbol> GetI32Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, [
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: I32, parameters: [], returnType: I32, body: Intrinsic.NegateI32),
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: I32,
                parameters: [new ParameterSymbol("right", I32)], returnType: I32, body: Intrinsic.SubtractI32),
        ]),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)], returnType: I32, body: Intrinsic.AddI32),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)], returnType: I32, body: Intrinsic.MultiplyI32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)], returnType: I32, body: Intrinsic.DivideI32),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)], returnType: Bool, body: Intrinsic.EqualsI32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)], returnType: Bool, body: Intrinsic.LessThanI32),

        new FunSymbol("ToString", receiverType: I32, parameters: [], returnType: String, body: Intrinsic.ToStringI32),
    ];

    private ImmutableArray<Symbol> GetI64Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, [
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: I64, parameters: [], returnType: I64, body: Intrinsic.NegateI64),
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: I64,
                parameters: [new ParameterSymbol("right", I64)], returnType: I64, body: Intrinsic.SubtractI64),
        ]),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: I64, body: Intrinsic.AddI64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: I64, body: Intrinsic.MultiplyI64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: I64, body: Intrinsic.DivideI64),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: Bool, body: Intrinsic.EqualsI64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: Bool, body: Intrinsic.LessThanI64),

        new FunSymbol("ToString", receiverType: I64, parameters: [], returnType: String, body: Intrinsic.ToStringI64),
    ];

    private ImmutableArray<Symbol> GetF32Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, [
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: F32, parameters: [], returnType: F32, body: Intrinsic.NegateF32),
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: F32,
                parameters: [new ParameterSymbol("right", F32)], returnType: F32, body: Intrinsic.SubtractF32),
        ]),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: F32, body: Intrinsic.AddF32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: F32, body: Intrinsic.MultiplyF32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: F32, body: Intrinsic.DivideF32),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: Bool, body: Intrinsic.EqualsF32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: Bool, body: Intrinsic.LessThanF32),

        new FunSymbol("ToString", receiverType: F32, parameters: [], returnType: String, body: Intrinsic.ToStringF32),
    ];

    private ImmutableArray<Symbol> GetF64Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, [
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: F64, parameters: [], returnType: F64, body: Intrinsic.NegateF64),
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: F64,
                parameters: [new ParameterSymbol("right", F64)], returnType: F64, body: Intrinsic.SubtractF64),
        ]),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: F64, body: Intrinsic.AddF64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: F64, body: Intrinsic.MultiplyF64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: F64, body: Intrinsic.DivideF64),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: Bool, body: Intrinsic.EqualsF64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: Bool, body: Intrinsic.LessThanF64),

        new FunSymbol("ToString", receiverType: F64, parameters: [], returnType: String, body: Intrinsic.ToStringF64),
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
        
        var b = new FunBuilder(this);
        b.Receiver(equal.ReceiverType);
        b.Param("right", equal.ParameterTypes[0]);
        b.Return(b.InstanceCall(notFun, b.InstanceCall(equal, b.Self, b.Arg0)));
        return b.ToFun(TokenKind.BangEqual);
    }

    private FunSymbol GenerateGreaterThan(FunSymbol lessThan)
    {
        var b = new FunBuilder(this);
        b.Receiver(lessThan.ParameterTypes[0]);
        b.Param("right", lessThan.ReceiverType!);
        b.Return(b.InstanceCall(lessThan, b.Arg0, b.Self));
        return b.ToFun(TokenKind.GreaterThan);
    }

    private FunSymbol GenerateComparisonOrEqual(FunSymbol comparison, FunSymbol equal, TokenKind name)
    {
        // self <= right = self < right || self == right
        // self >= right = self > right || self == right
        
        Debug.Assert(comparison.ReceiverType is not null);
        Debug.Assert(comparison.ReceiverType == equal.ReceiverType
                     && comparison.ParameterTypes.SequenceEqual(equal.ParameterTypes));
        
        var b = new FunBuilder(this);
        b.Receiver(comparison.ReceiverType);
        b.Param("right", comparison.ParameterTypes[0]);
        b.Return(b.Or(b.InstanceCall(comparison, b.Self, b.Arg0), b.InstanceCall(equal, b.Self, b.Arg0)));
        return b.ToFun(name);
    }
}