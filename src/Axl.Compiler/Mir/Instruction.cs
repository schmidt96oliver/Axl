using System.Collections.Frozen;
using System.Collections.Immutable;
using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax;

namespace Axl.Compiler.Mir;

public closed record Instruction(Place Target, ImmutableArray<Operand> Operands, SyntaxNode? Syntax)
{
    public sealed record Operator(OperatorKind Kind, Place Target, ImmutableArray<Operand> Arguments, SyntaxNode? Syntax)
        : Instruction(Target, Arguments.CastArray<Operand>(), Syntax);
    
    public sealed record VmCall(VmCallKind Kind, Place Target, ImmutableArray<Operand> Arguments, SyntaxNode? Syntax)
        : Instruction(Target, Arguments, Syntax);

    public sealed record Call(FunSymbol Fun, Place Target, ImmutableArray<Operand> Arguments, SyntaxNode? Syntax)
        : Instruction(Target, Arguments, Syntax);

    public sealed record StructInit(
        StructSymbol Struct,
        Place Target,
        FrozenDictionary<FieldSymbol, Operand> FieldValues,
        SyntaxNode? Syntax)
        : Instruction(Target, [.. FieldValues.Values], Syntax);

    public sealed record Assign(Place Target, Operand Value, SyntaxNode? Syntax = null) : Instruction(Target, [Value], Syntax);
}