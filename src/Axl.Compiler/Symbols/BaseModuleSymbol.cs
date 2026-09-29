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

    public TypeSymbol Never { get; }


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

        // Never is not nameable from code, so they will not become
        // members.
        Never = new TypeSymbol("Never", () => []);
    }


    private ImmutableArray<Symbol> GetFuns() =>
    [
        new FunSymbol("Print", receiverType: null, parameters: [new ParameterSymbol("text", String)], returnType: Unit, intrinsic: Intrinsic.Print)
    ];


    private ImmutableArray<Symbol> GetBoolMembers() =>
    [
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Bang)!, receiverType: Bool, parameters: [], returnType: Bool, intrinsic: Intrinsic.NotBool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: Bool,
            parameters: [new ParameterSymbol("right", Bool)], returnType: Bool, intrinsic: Intrinsic.EqualsBool),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, receiverType: Bool,
            parameters: [new ParameterSymbol("right", Bool)], returnType: Bool, intrinsic: Intrinsic.NotEqualsBool),

        new FunSymbol("ToString", receiverType: Bool, parameters: [], returnType: String, intrinsic: Intrinsic.ToStringBool),
    ];

    private ImmutableArray<Symbol> GetUnitMembers() =>
    [
        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: Unit,
            parameters: [new ParameterSymbol("right", Unit)], returnType: Bool, intrinsic: Intrinsic.EqualsUnit),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, receiverType: Unit,
            parameters: [new ParameterSymbol("right", Unit)], returnType: Bool, intrinsic: Intrinsic.NotEqualsUnit),
    ];

    private ImmutableArray<Symbol> GetStringMembers() =>
    [
        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: String,
            parameters: [new ParameterSymbol("right", String)], returnType: Bool, intrinsic: Intrinsic.EqualsString),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, receiverType: String,
            parameters: [new ParameterSymbol("right", String)], returnType: Bool, intrinsic: Intrinsic.NotEqualsString)
    ];

    private ImmutableArray<Symbol> GetI32Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, [
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: I32, parameters: [], returnType: I32, intrinsic: Intrinsic.NegateI32),
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: I32,
                parameters: [new ParameterSymbol("right", I32)], returnType: I32, intrinsic: Intrinsic.SubtractI32),
        ]),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)], returnType: I32, intrinsic: Intrinsic.AddI32),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)], returnType: I32, intrinsic: Intrinsic.MultiplyI32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)], returnType: I32, intrinsic: Intrinsic.DivideI32),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)], returnType: Bool, intrinsic: Intrinsic.EqualsI32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)], returnType: Bool, intrinsic: Intrinsic.NotEqualsI32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)], returnType: Bool, intrinsic: Intrinsic.LessThanI32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)],
            returnType: Bool, intrinsic: Intrinsic.LessThanOrEqualI32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)],
            returnType: Bool, intrinsic: Intrinsic.GreaterThanI32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!,
            receiverType: I32,
            parameters: [new ParameterSymbol("right", I32)], returnType: Bool, intrinsic: Intrinsic.GreaterThanOrEqualI32),

        new FunSymbol("ToString", receiverType: I32, parameters: [], returnType: String, intrinsic: Intrinsic.ToStringI32),
    ];

    private ImmutableArray<Symbol> GetI64Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, [
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: I64, parameters: [], returnType: I64, intrinsic: Intrinsic.NegateI64),
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: I64,
                parameters: [new ParameterSymbol("right", I64)], returnType: I64, intrinsic: Intrinsic.SubtractI64),
        ]),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: I64, intrinsic: Intrinsic.AddI64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: I64, intrinsic: Intrinsic.MultiplyI64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: I64, intrinsic: Intrinsic.DivideI64),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: Bool, intrinsic: Intrinsic.EqualsI64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: Bool, intrinsic: Intrinsic.NotEqualsI64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: Bool, intrinsic: Intrinsic.LessThanI64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: Bool, intrinsic: Intrinsic.LessThanOrEqualI64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, receiverType: I64,
            parameters: [new ParameterSymbol("right", I64)], returnType: Bool, intrinsic: Intrinsic.GreaterThanI64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!,
            receiverType: I64, parameters: [new ParameterSymbol("right", I64)], returnType: Bool, intrinsic: Intrinsic.GreaterThanOrEqualI64),

        new FunSymbol("ToString", receiverType: I64, parameters: [], returnType: String, intrinsic: Intrinsic.ToStringI64),
    ];

    private ImmutableArray<Symbol> GetF32Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, [
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: F32, parameters: [], returnType: F32, intrinsic: Intrinsic.NegateF32),
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: F32,
                parameters: [new ParameterSymbol("right", F32)], returnType: F32, intrinsic: Intrinsic.SubtractF32),
        ]),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: F32, intrinsic: Intrinsic.AddF32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: F32, intrinsic: Intrinsic.MultiplyF32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: F32, intrinsic: Intrinsic.DivideF32),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: Bool, intrinsic: Intrinsic.EqualsF32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: Bool, intrinsic: Intrinsic.NotEqualsF32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: Bool, intrinsic: Intrinsic.LessThanF32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: Bool, intrinsic: Intrinsic.LessThanOrEqualF32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, receiverType: F32,
            parameters: [new ParameterSymbol("right", F32)], returnType: Bool, intrinsic: Intrinsic.GreaterThanF32),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!,
            receiverType: F32, parameters: [new ParameterSymbol("right", F32)], returnType: Bool, intrinsic: Intrinsic.GreaterThanOrEqualF32),

        new FunSymbol("ToString", receiverType: F32, parameters: [], returnType: String, intrinsic: Intrinsic.ToStringF32),
    ];

    private ImmutableArray<Symbol> GetF64Members() =>
    [
        new FunGroupSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, [
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: F64, parameters: [], returnType: F64, intrinsic: Intrinsic.NegateF64),
            new FunSymbol(SyntaxFacts.GetText(TokenKind.Minus)!, receiverType: F64,
                parameters: [new ParameterSymbol("right", F64)], returnType: F64, intrinsic: Intrinsic.SubtractF64),
        ]),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.Plus)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: F64, intrinsic: Intrinsic.AddF64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Star)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: F64, intrinsic: Intrinsic.MultiplyF64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.Slash)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: F64, intrinsic: Intrinsic.DivideF64),

        new FunSymbol(SyntaxFacts.GetText(TokenKind.DoubleEqual)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: Bool, intrinsic: Intrinsic.EqualsF64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.BangEqual)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: Bool, intrinsic: Intrinsic.NotEqualsF64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThan)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: Bool, intrinsic: Intrinsic.LessThanF64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.LessThanEqual)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: Bool, intrinsic: Intrinsic.LessThanOrEqualF64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThan)!, receiverType: F64,
            parameters: [new ParameterSymbol("right", F64)], returnType: Bool, intrinsic: Intrinsic.GreaterThanF64),
        new FunSymbol(SyntaxFacts.GetText(TokenKind.GreaterThanEqual)!,
            receiverType: F64, parameters: [new ParameterSymbol("right", F64)], returnType: Bool, intrinsic: Intrinsic.GreaterThanOrEqualF64),

        new FunSymbol("ToString", receiverType: F64, parameters: [], returnType: String, intrinsic: Intrinsic.ToStringF64),
    ];

}