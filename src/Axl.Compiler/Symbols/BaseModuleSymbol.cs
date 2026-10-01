using System.Collections.Immutable;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Symbols;

public sealed class BaseModuleSymbol : ModuleOrTypeSymbol
{
    public override SymbolKind Kind => SymbolKind.Module;

    public override ImmutableArray<Symbol> Members { get; }

    public TypeSymbol I32 { get; }
    public TypeSymbol I64 { get; }
    public TypeSymbol F32 { get; }
    public TypeSymbol F64 { get; }
    public TypeSymbol String { get; }
    public TypeSymbol Bool { get; }
    public TypeSymbol Unit { get; }

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

        Members = [I32, I64, F32, F64, Bool, String, Unit, .. funs];
    }


    private ImmutableArray<Symbol> GetFuns() =>
    [
        new FunSymbol("Print", receiverType: null, parameters: [new ParameterSymbol("text", String)], returnType: Unit, body: Intrinsic.Print)
    ];


    private ImmutableArray<Symbol> GetBoolMembers() =>
    [
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Bang)!, receiverType: Bool, parameters: [], returnType: Bool, body: Intrinsic.NotBool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: Bool,
            parameters: [new ParameterSymbol("right", Bool)], returnType: Bool, body: Intrinsic.EqualsBool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, receiverType: Bool,
            parameters: [new ParameterSymbol("right", Bool)], returnType: Bool, body: Intrinsic.NotEqualsBool),

        new FunSymbol("ToString", receiverType: Bool, parameters: [], returnType: String, body: Intrinsic.ToStringBool),
    ];

    private ImmutableArray<Symbol> GetUnitMembers() =>
    [
        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: Unit,
            parameters: [new ParameterSymbol("right", Unit)], returnType: Bool, body: Intrinsic.EqualsUnit),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, receiverType: Unit,
            parameters: [new ParameterSymbol("right", Unit)], returnType: Bool, body: Intrinsic.NotEqualsUnit),
    ];

    private ImmutableArray<Symbol> GetStringMembers() =>
    [
        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: String,
            parameters: [new ParameterSymbol("right", String)], returnType: Bool, body: Intrinsic.EqualsString),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, receiverType: String,
            parameters: [new ParameterSymbol("right", String)], returnType: Bool, body: Intrinsic.NotEqualsString)
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
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)], returnType: Bool, body: Intrinsic.NotEqualsI32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)], returnType: Bool, body: Intrinsic.LessThanI32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)],
            returnType: Bool, body: Intrinsic.LessThanOrEqualI32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)],
            returnType: Bool, body: Intrinsic.GreaterThanI32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!,
            receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)], returnType: Bool, body: Intrinsic.GreaterThanOrEqualI32),

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
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: Bool, body: Intrinsic.NotEqualsI64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: Bool, body: Intrinsic.LessThanI64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: Bool, body: Intrinsic.LessThanOrEqualI64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: Bool, body: Intrinsic.GreaterThanI64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!,
            receiverType: I64, parameters: [new ParameterSymbol("right", I64)], returnType: Bool, body: Intrinsic.GreaterThanOrEqualI64),

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
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: Bool, body: Intrinsic.NotEqualsF32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: Bool, body: Intrinsic.LessThanF32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: Bool, body: Intrinsic.LessThanOrEqualF32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: Bool, body: Intrinsic.GreaterThanF32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!,
            receiverType: F32, parameters: [new ParameterSymbol("right", F32)], returnType: Bool, body: Intrinsic.GreaterThanOrEqualF32),

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
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: Bool, body: Intrinsic.NotEqualsF64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: Bool, body: Intrinsic.LessThanF64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: Bool, body: Intrinsic.LessThanOrEqualF64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: Bool, body: Intrinsic.GreaterThanF64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!,
            receiverType: F64, parameters: [new ParameterSymbol("right", F64)], returnType: Bool, body: Intrinsic.GreaterThanOrEqualF64),

        new FunSymbol("ToString", receiverType: F64, parameters: [], returnType: String, body: Intrinsic.ToStringF64),
    ];

}